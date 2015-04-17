package container

import (
	"encoding/json"
	"fmt"
	"io"
	"io/ioutil"
	"net/http"

	"errors"
	"strconv"
	"strings"

	"github.com/cloudfoundry-incubator/garden"
	"github.com/cloudfoundry-incubator/garden-windows/containerizer_url"
	"github.com/cloudfoundry-incubator/garden-windows/process"

	"github.com/gorilla/websocket"
	"github.com/pivotal-golang/lager"
)

type container struct {
	containerizerURL *containerizer_url.ContainerizerURL
	handle           string
	logger           lager.Logger
}

type netInResponse struct {
	HostPort    uint32 `json:"hostPort"`
	ErrorString string `json:"error"`
}

type ProcessStreamEvent struct {
	MessageType    string             `json:"type"`
	ApiProcessSpec garden.ProcessSpec `json:"pspec"`
	Data           string             `json:"data"`
}

func NewContainer(containerizerURL *containerizer_url.ContainerizerURL, handle string, logger lager.Logger) *container {
	return &container{
		containerizerURL: containerizerURL,
		handle:           handle,
		logger:           logger,
	}
}

var ErrReadFromPath = errors.New("could not read tar path")

func (container *container) Handle() string {
	return container.handle
}

func (container *container) Stop(kill bool) error {
	response, err := http.Post(container.containerizerURL.Stop(container.Handle()), "text/plain", nil)
	if err != nil {
		return err
	}
	if response.StatusCode != http.StatusOK {
		return errors.New(response.Status)
	}
	return nil
}

func (container *container) parseJson(response *http.Response, err error, output interface{}) error {
	if err != nil {
		container.logger.Info("ERROR GETTING JSON", lager.Data{
			"url":   response.Request.URL.String(),
			"error": err,
		})
		return err
	}
	defer response.Body.Close()
	rawJSON, err := ioutil.ReadAll(response.Body)
	if err != nil {
		return err
	}

	if response.StatusCode != http.StatusOK {
		type exceptionStruct struct {
			ExceptionMessage string
		}
		exceptionData := exceptionStruct{}

		errorJson := json.Unmarshal(rawJSON, &exceptionData)
		if errorJson != nil || exceptionData.ExceptionMessage == "" {
			return errors.New(response.Status)
		}
		return errors.New(exceptionData.ExceptionMessage)
	}

	err = json.Unmarshal(rawJSON, output)
	if err != nil {
		container.logger.Info("ERROR UNMARSHALING JSON", lager.Data{
			"url":   response.Request.URL.String(),
			"error": err,
		})
		return err
	}
	return nil
}

func (container *container) GetProperties() (garden.Properties, error) {
	url := container.containerizerURL.GetProperties(container.Handle())
	properties := garden.Properties{}
	response, err := http.Get(url)
	err = container.parseJson(response, err, &properties)
	return properties, err
}

func (container *container) Info() (garden.ContainerInfo, error) {
	url := container.containerizerURL.Info(container.Handle())
	response, err := http.Get(url)
	info := garden.ContainerInfo{}
	err = container.parseJson(response, err, &info)
	return info, err
}

func (container *container) StreamIn(dstPath string, tarStream io.Reader) error {
	url := container.containerizerURL.StreamIn(container.Handle(), dstPath)
	req, err := http.NewRequest("PUT", url, tarStream)
	if err != nil {
		return err
	}
	req.Header.Set("Content-Type", "application/octet-stream")
	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		return err
	}
	resp.Body.Close()
	if resp.StatusCode != http.StatusOK {
		return errors.New(resp.Status)
	}
	return nil
}

func (container *container) StreamOut(srcPath string) (io.ReadCloser, error) {
	url := container.containerizerURL.StreamOut(container.Handle(), srcPath)
	resp, err := http.Get(url)
	if err != nil {
		return nil, err
	}
	if resp.StatusCode != http.StatusOK {
		return nil, ErrReadFromPath
	}
	return resp.Body, nil
}

func (container *container) LimitBandwidth(limits garden.BandwidthLimits) error {
	return nil
}
func (container *container) CurrentBandwidthLimits() (garden.BandwidthLimits, error) {
	return garden.BandwidthLimits{}, nil
}

func (container *container) LimitCPU(limits garden.CPULimits) error {
	return nil
}

func (container *container) CurrentCPULimits() (garden.CPULimits, error) {
	return garden.CPULimits{}, nil
}

func (container *container) LimitDisk(limits garden.DiskLimits) error {
	return nil
}
func (container *container) CurrentDiskLimits() (garden.DiskLimits, error) {
	return garden.DiskLimits{}, nil
}

func (container *container) LimitMemory(limits garden.MemoryLimits) error {
	url := container.containerizerURL.MemoryLimit(container.Handle())
	jsonLimits, err := json.Marshal(limits)
	if err != nil {
		return err
	}
	response, err := http.Post(url, "application/json", strings.NewReader(string(jsonLimits)))
	if err != nil {
		return err
	}
	response.Body.Close()

	if response.StatusCode != 200 {
		return errors.New(response.Status)
	}
	return nil
}

func (container *container) CurrentMemoryLimits() (garden.MemoryLimits, error) {
	url := container.containerizerURL.MemoryLimit(container.Handle())
	limits := garden.MemoryLimits{}
	response, err := http.Get(url)
	err = container.parseJson(response, err, &limits)
	return limits, err
}

func (container *container) NetIn(hostPort, containerPort uint32) (uint32, uint32, error) {
	url := container.containerizerURL.NetIn(container.Handle())
	response, err := http.Post(url, "application/json", strings.NewReader(fmt.Sprintf(`{"hostPort": %v}`, hostPort)))
	var responseJSON netInResponse

	err = container.parseJson(response, err, &responseJSON)

	if err != nil {
		return 0, 0, err
	}
	if responseJSON.ErrorString != "" {
		return 0, 0, errors.New(responseJSON.ErrorString)
	}

	return responseJSON.HostPort, containerPort, err
}

func (container *container) NetOut(rule garden.NetOutRule) error {
	return nil
}

func (container *container) Run(processSpec garden.ProcessSpec, processIO garden.ProcessIO) (garden.Process, error) {
	wsUri := container.containerizerURL.Run(container.Handle())
	ws, _, err := websocket.DefaultDialer.Dial(wsUri, nil)
	if err != nil {
		return nil, err
	}
	websocket.WriteJSON(ws, ProcessStreamEvent{
		MessageType:    "run",
		ApiProcessSpec: processSpec,
	})

	proc := process.NewDotNetProcess()
	pidChannel := make(chan uint32)

	streamWebsocketIOToContainerizer(ws, processIO)
	go func() {
		exitCode, err := streamWebsocketIOFromContainerizer(ws, pidChannel, processIO)
		proc.StreamOpen <- process.DotNetProcessExitStatus{exitCode, err}
		close(proc.StreamOpen)
	}()

	proc.Pid = <-pidChannel

	return proc, nil
}

func (container *container) Attach(uint32, garden.ProcessIO) (garden.Process, error) {
	return process.NewDotNetProcess(), nil
}

func (container *container) Metrics() (garden.Metrics, error) {
	return garden.Metrics{}, nil
}

func (container *container) GetProperty(name string) (string, error) {
	requestUrl := container.containerizerURL.GetProperty(container.Handle(), name)
	container.logger.Info("GET PROPERTY", lager.Data{
		"property": name,
		"url":      requestUrl,
	})
	response, err := http.Get(requestUrl)
	if err != nil {
		container.logger.Info("NO GET FOR YOU!", lager.Data{
			"error": err,
		})
		return "", err
	}
	defer response.Body.Close()

	var property string
	err = json.NewDecoder(response.Body).Decode(&property)
	if err != nil {
		return "", err
	}

	return property, nil
}

func (container *container) SetProperty(name string, value string) error {
	requestUrl := container.containerizerURL.SetProperty(container.Handle(), name)
	container.logger.Info("SET PROPERTY", lager.Data{
		"property": name,
		"url":      requestUrl,
	})
	request, err := http.NewRequest("PUT", requestUrl, strings.NewReader(value))
	if err != nil {
		return err
	}
	request.Header.Set("Content-Type", "application/json")
	response, err := http.DefaultClient.Do(request)
	if err != nil {
		return err
	}

	response.Body.Close()
	return nil
}

func (container *container) RemoveProperty(name string) error {
	requestUrl := container.containerizerURL.RemoveProperty(container.Handle(), name)
	container.logger.Info("REMOVING PROPERTY", lager.Data{
		"property": name,
		"url":      requestUrl,
	})
	request, err := http.NewRequest("DELETE", requestUrl, strings.NewReader(""))
	if err != nil {
		return err
	}
	response, err := http.DefaultClient.Do(request)
	if err != nil {
		return err
	}

	response.Body.Close()
	return nil
}

func streamWebsocketIOToContainerizer(ws *websocket.Conn, processIO garden.ProcessIO) {
	if processIO.Stdin != nil {
		fiw := faninWriter{
			hasSink: make(chan struct{}),
		}
		fiw.AddSink(ws)
		fiw.AddSource(processIO.Stdin)
	}
}

func streamWebsocketIOFromContainerizer(ws *websocket.Conn, pidChannel chan<- uint32, processIO garden.ProcessIO) (int, error) {
	// CLOSE WS SOMEWHERE ;; defer ws.Close() ;; FIXME
	defer close(pidChannel)

	receiveStream := ProcessStreamEvent{}
	for {
		err := websocket.ReadJSON(ws, &receiveStream)
		if err != nil {
			return -1, err
		}

		if receiveStream.MessageType == "pid" {
			pid, err := strconv.ParseInt(receiveStream.Data, 10, 32)
			if err != nil {
				return -1, err
			}
			pidChannel <- uint32(pid)
		}

		if receiveStream.MessageType == "stdout" && processIO.Stdout != nil {
			io.WriteString(processIO.Stdout, receiveStream.Data)
		}
		if receiveStream.MessageType == "stderr" && processIO.Stderr != nil {
			io.WriteString(processIO.Stderr, receiveStream.Data)
		}

		if receiveStream.MessageType == "error" {
			return -1, errors.New(receiveStream.Data)
		}
		if receiveStream.MessageType == "close" {
			exitCode, err := strconv.Atoi(receiveStream.Data)
			if err != nil {
				return -1, err
			}
			return exitCode, nil
		}
	}
}
