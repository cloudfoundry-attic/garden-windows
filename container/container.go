package container

import (
	"fmt"
	"io"
	"net/url"

	"errors"
	"strconv"
	"strings"

	"github.com/cloudfoundry-incubator/garden"
	"github.com/cloudfoundry-incubator/garden-windows/dotnet"
	"github.com/cloudfoundry-incubator/garden-windows/process"

	"github.com/gorilla/websocket"
	"github.com/pivotal-golang/lager"
)

type container struct {
	handle string
	logger lager.Logger
	client *dotnet.Client
}

type ports struct {
	HostPort      uint32 `json:"hostPort,omitempty"`
	ContainerPort uint32 `json:"containerPort,omitempty"`
	ErrorString   string `json:"error,omitempty"`
}

type ProcessStreamEvent struct {
	MessageType    string             `json:"type"`
	ApiProcessSpec garden.ProcessSpec `json:"pspec"`
	Data           string             `json:"data"`
}

func NewContainer(client *dotnet.Client, handle string, logger lager.Logger) *container {
	return &container{
		client: client,
		handle: handle,
		logger: logger,
	}
}

var ErrReadFromPath = errors.New("could not read tar path")

func (container *container) Handle() string {
	return container.handle
}

type UndefinedPropertyError struct {
	Handle string
	Key    string
}

func (err UndefinedPropertyError) Error() string {
	return fmt.Sprintf("property does not exist: %s", err.Key)
}

func (container *container) Stop(kill bool) error {
	url := fmt.Sprintf("/api/containers/%s/stop", container.Handle())
	return container.client.Post(url, nil, nil)
}

func (container *container) Info() (garden.ContainerInfo, error) {
	url := fmt.Sprintf("/api/containers/%s/info", container.Handle())
	info := garden.ContainerInfo{}
	err := container.client.Get(url, &info)
	return info, err
}

func (container *container) streamUrl(paramName, fileName string) string {
	base := url.URL{}
	base.Path = fmt.Sprintf("/api/containers/%s/files", container.Handle())
	q := base.Query()
	q.Add(paramName, fileName)
	base.RawQuery = q.Encode()
	return base.String()
}

func (container *container) StreamIn(dstPath string, tarStream io.Reader) error {
	url := container.streamUrl("destination", dstPath)
	return container.client.Put(url, tarStream, "application/octet-stream")
}

func (container *container) StreamOut(srcPath string) (io.ReadCloser, error) {
	url := container.streamUrl("source", srcPath)
	return container.client.ReadBody(url)
}

func (container *container) LimitBandwidth(limits garden.BandwidthLimits) error {
	return nil
}
func (container *container) CurrentBandwidthLimits() (garden.BandwidthLimits, error) {
	return garden.BandwidthLimits{}, nil
}

func (container *container) LimitCPU(limits garden.CPULimits) error {
	url := fmt.Sprintf("/api/containers/%s/cpu_limit", container.Handle())
	err := container.client.Post(url, limits, nil)
	return err
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
	url := fmt.Sprintf("/api/containers/%s/memory_limit", container.Handle())
	return container.client.Post(url, limits, nil)
}

func (container *container) CurrentMemoryLimits() (garden.MemoryLimits, error) {
	url := fmt.Sprintf("/api/containers/%s/memory_limit", container.Handle())
	limits := garden.MemoryLimits{}
	err := container.client.Get(url, &limits)
	return limits, err
}

func (container *container) NetIn(hostPort, containerPort uint32) (uint32, uint32, error) {
	url := fmt.Sprintf("/api/containers/%s/net/in", container.Handle())
	netInResponse := ports{HostPort: hostPort, ContainerPort: containerPort}
	err := container.client.Post(url, netInResponse, &netInResponse)

	if err == nil && netInResponse.ErrorString != "" {
		err = errors.New(netInResponse.ErrorString)
	}

	return netInResponse.HostPort, containerPort, err
}

func (container *container) NetOut(rule garden.NetOutRule) error {
	url := fmt.Sprintf("/api/containers/%s/net/out", container.Handle())
	return container.client.Post(url, rule, nil)
}

func (container *container) Run(processSpec garden.ProcessSpec, processIO garden.ProcessIO) (garden.Process, error) {
	wsUri := container.client.RunURL(container.Handle())
	ws, _, err := websocket.DefaultDialer.Dial(wsUri, nil)
	if err != nil {
		return nil, err
	}
	websocket.WriteJSON(ws, ProcessStreamEvent{
		MessageType:    "run",
		ApiProcessSpec: processSpec,
	})

	proc := process.NewDotNetProcess(container.Handle(), container.client)
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
	return process.NewDotNetProcess(container.Handle(), container.client), nil
}

func (container *container) Metrics() (garden.Metrics, error) {
	url := fmt.Sprintf("/api/containers/%s/metrics", container.Handle())
	metrics := garden.Metrics{}
	err := container.client.Get(url, &metrics)
	return metrics, err
}

func (container *container) Property(name string) (string, error) {
	url := fmt.Sprintf("/api/containers/%s/properties/%s", container.Handle(), name)
	var property string
	err := container.client.Get(url, &property)
	return property, err
}

func (container *container) Properties() (garden.Properties, error) {
	url := fmt.Sprintf("/api/containers/%s/properties", container.Handle())
	properties := garden.Properties{}
	err := container.client.Get(url, &properties)
	return properties, err
}

func (container *container) SetProperty(name string, value string) error {
	url := fmt.Sprintf("/api/containers/%s/properties/%s", container.Handle(), name)
	return container.client.Put(url, strings.NewReader(value), "application/json")
}

func (container *container) RemoveProperty(name string) error {
	url := fmt.Sprintf("/api/containers/%s/properties/%s", container.Handle(), name)
	return container.client.Delete(url)
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
