package backend

import (
	"time"

	"github.com/cloudfoundry-incubator/garden/api"
	"github.com/pivotal-cf/netgarden/container"
)

type DotNetBackend struct{}

func (backend DotNetBackend) Start() error {
	return nil
}

func (backend DotNetBackend) Stop() {}

func (backend DotNetBackend) GraceTime(api.Container) time.Duration {
	return time.Second
}

func (backend DotNetBackend) Ping() error {
	return nil
}

func (backend DotNetBackend) Capacity() (api.Capacity, error) {
	capacity := api.Capacity{}
	return capacity, nil
}

func (backend DotNetBackend) Create(api.ContainerSpec) (api.Container, error) {
	netContainer := container.DotNetContainer{}
	return netContainer, nil
}

func (backend DotNetBackend) Destroy(handle string) error {
	return nil
}

func (backend DotNetBackend) Containers(api.Properties) ([]api.Container, error) {
	containers := []api.Container{}
	return containers, nil
}

func (backend DotNetBackend) Lookup(handle string) (api.Container, error) {
	netContainer := container.DotNetContainer{}
	return netContainer, nil
}
