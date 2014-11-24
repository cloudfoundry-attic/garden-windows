package container

import (
	"io"
	"log"
	"net/http"
	"net/url"

	"github.com/cloudfoundry-incubator/garden/api"
	"github.com/pivotal-cf-experimental/garden-dot-net/process"

	"code.google.com/p/go.net/websocket"
)

type container struct {
	containerizerURL url.URL
	handle           string
}

type ProcessStreamEvent struct {
	MessageType    string
	ApiProcessSpec api.ProcessSpec
	Data           string
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

	req, err := http.NewRequest("PUT", url, tarStream)
	if err != nil {
		return err
	}
	_, err = http.DefaultClient.Do(req)

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
	// FIXME ; probably not even good for this mock
	return 80, 80, nil
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
	wsUri := container.containerizerWS() + "/api/run"
	ws, err := websocket.Dial(wsUri, "", origin)
	if err != nil {
		log.Fatal(err)
	}
	websocket.JSON.Send(ws, ProcessStreamEvent{
		MessageType:    "run",
		ApiProcessSpec: processSpec,
	})
	return process.DotNetProcess{}, nil
}

func (container *container) Attach(uint32, api.ProcessIO) (api.Process, error) {
	return process.DotNetProcess{}, nil
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
