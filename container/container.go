package container

import (
	"fmt"
	"io"
	"io/ioutil"
	"net/http"
	"net/url"

	"encoding/json"
	"errors"
	"strings"

	"github.com/cloudfoundry-incubator/garden/api"
	"github.com/pivotal-cf-experimental/garden-dot-net/process"

	"code.google.com/p/go.net/websocket"
)

type container struct {
	containerizerURL url.URL
	handle           string
}

type netInResponse struct {
	HostPort    uint32 `json:"hostPort"`
	ErrorString string `json:"error"`
}

type ProcessStreamEvent struct {
	MessageType    string          `json:"type"`
	ApiProcessSpec api.ProcessSpec `json:"pspec"`
	Data           string          `json:"data"`
}

func NewContainer(containerizerURL url.URL, handle string) *container {
	return &container{
		containerizerURL: containerizerURL,
		handle:           handle,
	}
}

func (container *container) Handle() string {
	return container.handle
}

func (container *container) Stop(kill bool) error {
	return nil
}

func (container *container) Info() (api.ContainerInfo, error) {
	return api.ContainerInfo{}, nil
}

func (container *container) StreamIn(dstPath string, tarStream io.Reader) error {
	url := container.containerizerURL.String() + "/api/containers/" + container.Handle() + "/files?destination=" + dstPath

	_, err := http.Post(url, "application/octet-stream", tarStream)

	return err
}
func (container *container) StreamOut(srcPath string) (io.ReadCloser, error) {
	url := container.containerizerURL.String() + "/api/containers/" + container.Handle() + "/files?source=" + srcPath
	resp, err := http.Get(url)
	if err != nil {
		return nil, err
	}
	return resp.Body, nil
}

func (container *container) LimitBandwidth(limits api.BandwidthLimits) error {
	return nil
}
func (container *container) CurrentBandwidthLimits() (api.BandwidthLimits, error) {
	return api.BandwidthLimits{}, nil
}

func (container *container) LimitCPU(limits api.CPULimits) error {
	return nil
}

func (container *container) CurrentCPULimits() (api.CPULimits, error) {
	return api.CPULimits{}, nil
}

func (container *container) LimitDisk(limits api.DiskLimits) error {
	return nil
}
func (container *container) CurrentDiskLimits() (api.DiskLimits, error) {
	return api.DiskLimits{}, nil
}

func (container *container) LimitMemory(limits api.MemoryLimits) error {
	return nil
}
func (container *container) CurrentMemoryLimits() (api.MemoryLimits, error) {
	return api.MemoryLimits{}, nil
}

func (container *container) NetIn(hostPort, containerPort uint32) (uint32, uint32, error) {
	url := container.containerizerURL.String() + "/api/containers/" + container.Handle() + "/net/in"
	response, err := http.Post(url, "application/json", strings.NewReader(fmt.Sprintf(`{"hostPort": %v}`, hostPort)))
	if err != nil {
		return 0, 0, err
	}
	responseBody, err := ioutil.ReadAll(response.Body)
	if err != nil {
		return 0, 0, err
	}

	var responseJSON netInResponse
	err = json.Unmarshal(responseBody, &responseJSON)
	if err != nil {
		return 0, 0, err
	}

	if responseJSON.ErrorString != "" {
		return 0, 0, errors.New(responseJSON.ErrorString)
	}

	return responseJSON.HostPort, containerPort, err
}

func (container *container) NetOut(network string, port uint32) error {
	return nil
}

func (container *container) containerizerWS() string {
	u2 := container.containerizerURL
	u2.Scheme = "ws"
	return u2.String()
}

func (container *container) Run(processSpec api.ProcessSpec, processIO api.ProcessIO) (api.Process, error) {
	origin := "http://localhost/"
	wsUri := container.containerizerWS() + "/api/containers/" + container.handle + "/run"
	ws, err := websocket.Dial(wsUri, "", origin)
	if err != nil {
		return nil, err
	}
	websocket.JSON.Send(ws, ProcessStreamEvent{
		MessageType:    "run",
		ApiProcessSpec: processSpec,
	})

	proc := process.NewDotNetProcess()

	streamWebsocketIOToContainerizer(ws, processIO)
	go func() {
		err := streamWebsocketIOFromContainerizer(ws, processIO)
		if err != nil {
			proc.StreamOpen <- err.Error()
		}
		close(proc.StreamOpen)
	}()

	return proc, nil
}

func (container *container) Attach(uint32, api.ProcessIO) (api.Process, error) {
	return process.NewDotNetProcess(), nil
}

func (container *container) GetProperty(name string) (string, error) {
	return "A Property Value", nil
}

func (container *container) SetProperty(name string, value string) error {
	return nil
}
func (container *container) RemoveProperty(name string) error {
	return nil
}

func streamWebsocketIOToContainerizer(ws *websocket.Conn, processIO api.ProcessIO) {
	if processIO.Stdin != nil {
		fiw := faninWriter{
			hasSink: make(chan struct{}),
		}
		fiw.AddSink(ws)
		fiw.AddSource(processIO.Stdin)
	}
}

func streamWebsocketIOFromContainerizer(ws *websocket.Conn, processIO api.ProcessIO) error {
	receiveStream := ProcessStreamEvent{}
	for {
		err := websocket.JSON.Receive(ws, &receiveStream)
		if err != nil {
			return err
		}

		if receiveStream.MessageType == "stdout" && processIO.Stdout != nil {
			io.WriteString(processIO.Stdout, receiveStream.Data)
		}
		if receiveStream.MessageType == "stderr" && processIO.Stderr != nil {
			io.WriteString(processIO.Stderr, receiveStream.Data)
		}

		if receiveStream.MessageType == "error" {
			return errors.New(receiveStream.Data)
		}
		if receiveStream.MessageType == "close" {
			return nil
		}
	}
}
