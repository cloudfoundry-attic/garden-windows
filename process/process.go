package process

import "github.com/cloudfoundry-incubator/garden/api"

type DotNetProcess struct{}

func (process DotNetProcess) ID() uint32 {
	return 0
}
func (process DotNetProcess) Wait() (int, error) {
	return 0, nil
}
func (process DotNetProcess) SetTTY(ttyspec api.TTYSpec) error {
	return nil
}
