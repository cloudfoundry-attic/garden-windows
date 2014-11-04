package backend

import (
	"encoding/json"
	"net/http"
	"time"

	"github.com/cloudfoundry-incubator/garden/api"
	"github.com/pivotal-cf-experimental/garden-dot-net/container"
	"strings"
)

type DotNetBackend struct {
	TupperwareURL string
}

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

func (backend DotNetBackend) Create(containerSpec api.ContainerSpec) (api.Container, error) {
	netContainer := container.DotNetContainer{}
	url := backend.TupperwareURL + "/api/containers"
	containerSpecJSON, err := json.Marshal(containerSpec)
	if err != nil {
		return nil, err
	}
	_, err = http.Post(url, "application/json", strings.NewReader(string(containerSpecJSON)))
	if err != nil {
		return netContainer, err
	}
	return netContainer, nil
}

func (backend DotNetBackend) Destroy(handle string) error {
	url := backend.TupperwareURL + "/api/containers/" + handle

	req, err := http.NewRequest("DELETE", url, nil)
	if err != nil {
		return err
	}
	_, err = http.DefaultClient.Do(req)

	return err
}

func (backend DotNetBackend) Containers(api.Properties) ([]api.Container, error) {
	containers := []api.Container{
		container.DotNetContainer{},
		container.DotNetContainer{},
		container.DotNetContainer{},
		container.DotNetContainer{},
	}
	return containers, nil
}

func (backend DotNetBackend) Lookup(handle string) (api.Container, error) {
	netContainer := container.DotNetContainer{}
	return netContainer, nil
}
