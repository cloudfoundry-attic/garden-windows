package container

import (
	"io"

	"github.com/cloudfoundry-incubator/garden/api"
	"github.com/pivotal-cf/netgarden/process"
)

type DotNetContainer struct{}

func (container DotNetContainer) Handle() string {
	return "a dotnetcontainer handle"
}

func (container DotNetContainer) Stop(kill bool) error {
	return nil
}

func (container DotNetContainer) Info() (api.ContainerInfo, error) {
	return api.ContainerInfo{}, nil
}

func (container DotNetContainer) StreamIn(dstPath string, tarStream io.Reader) error {
	return nil
}
func (container DotNetContainer) StreamOut(srcPath string) (io.ReadCloser, error) {
	// FIXME: is this sufficient??? I assume not
	return nil, nil
}

func (container DotNetContainer) LimitBandwidth(limits api.BandwidthLimits) error {
	return nil
}
func (container DotNetContainer) CurrentBandwidthLimits() (api.BandwidthLimits, error) {
	return api.BandwidthLimits{}, nil
}

func (container DotNetContainer) LimitCPU(limits api.CPULimits) error {
	return nil
}
func (container DotNetContainer) CurrentCPULimits() (api.CPULimits, error) {
	return api.CPULimits{}, nil
}

func (container DotNetContainer) LimitDisk(limits api.DiskLimits) error {
	return nil
}
func (container DotNetContainer) CurrentDiskLimits() (api.DiskLimits, error) {
	return api.DiskLimits{}, nil
}

func (container DotNetContainer) LimitMemory(limits api.MemoryLimits) error {
	return nil
}
func (container DotNetContainer) CurrentMemoryLimits() (api.MemoryLimits, error) {
	return api.MemoryLimits{}, nil
}

func (container DotNetContainer) NetIn(hostPort, containerPort uint32) (uint32, uint32, error) {
	// FIXME ; probably not even good for this mock
	return 80, 80, nil
}
func (container DotNetContainer) NetOut(network string, port uint32) error {
	return nil
}

func (container DotNetContainer) Run(api.ProcessSpec, api.ProcessIO) (api.Process, error) {
	return process.DotNetProcess{}, nil
}
func (container DotNetContainer) Attach(uint32, api.ProcessIO) (api.Process, error) {
	return process.DotNetProcess{}, nil
}

func (container DotNetContainer) GetProperty(name string) (string, error) {
	return "", nil
}
func (container DotNetContainer) SetProperty(name string, value string) error {
	return nil
}
func (container DotNetContainer) RemoveProperty(name string) error {
	return nil
}
