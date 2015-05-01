package process

import (
	"fmt"

	"github.com/cloudfoundry-incubator/garden"
	"github.com/cloudfoundry-incubator/garden-windows/dotnet"
)

type DotNetProcessExitStatus struct {
	ExitCode int
	Err      error
}

type DotNetProcess struct {
	Pid             uint32
	StreamOpen      chan DotNetProcessExitStatus
	containerHandle string
	client          *dotnet.Client
}

func NewDotNetProcess(containerHandle string, client *dotnet.Client) DotNetProcess {
	return DotNetProcess{
		StreamOpen:      make(chan DotNetProcessExitStatus),
		containerHandle: containerHandle,
		client:          client,
	}
}

func (process DotNetProcess) ID() uint32 {
	return process.Pid
}

func (process DotNetProcess) Wait() (int, error) {
	exitStatus := <-process.StreamOpen
	return exitStatus.ExitCode, exitStatus.Err
}

func (process DotNetProcess) SetTTY(ttyspec garden.TTYSpec) error {
	return nil
}

func (process DotNetProcess) Signal(signal garden.Signal) error {
	url := fmt.Sprintf("/api/containers/%s/processes/%d", process.containerHandle, process.Pid)
	return process.client.Delete(url)
}
