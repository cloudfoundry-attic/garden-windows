package process

import "github.com/cloudfoundry-incubator/garden"

type DotNetProcessExitStatus struct {
	ExitCode int
	Err      error
}

type DotNetProcess struct {
	Pid        uint32
	StreamOpen chan DotNetProcessExitStatus
}

func NewDotNetProcess() DotNetProcess {
	return DotNetProcess{
		StreamOpen: make(chan DotNetProcessExitStatus),
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
	return nil
}
