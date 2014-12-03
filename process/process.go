package process

import (
	"errors"

	"github.com/cloudfoundry-incubator/garden/api"
)

type DotNetProcess struct {
	StreamOpen chan string
}

func NewDotNetProcess() DotNetProcess {
	return DotNetProcess{
		StreamOpen: make(chan string),
	}
}

func (process DotNetProcess) ID() uint32 {
	return 0
}

func (process DotNetProcess) Wait() (int, error) {
	errMessage := <-process.StreamOpen
	if errMessage != "" {
		return 0, errors.New(errMessage)
	}

	return 0, nil
}

func (process DotNetProcess) SetTTY(ttyspec api.TTYSpec) error {
	return nil
}
