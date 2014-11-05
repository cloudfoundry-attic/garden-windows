package backend

import (
	"encoding/json"
	"net/http"
	"time"

	"github.com/cloudfoundry-incubator/garden/api"
	"github.com/pivotal-cf-experimental/garden-dot-net/container"
	"strings"
)

type dotNetBackend struct {
	tupperwareURL string
}

func NewDotNetBackend(tupperwareURL string) *dotNetBackend {
	return &dotNetBackend{
		tupperwareURL: tupperwareURL,
	}
}

func (dotNetBackend *dotNetBackend) TupperwareURL() string {
	return dotNetBackend.tupperwareURL
}

func (dotNetBackend *dotNetBackend) Start() error {
	return nil
}

func (dotNetBackend *dotNetBackend) Stop() {}

func (dotNetBackend *dotNetBackend) GraceTime(api.Container) time.Duration {
	return time.Second
}

func (dotNetBackend *dotNetBackend) Ping() error {
	return nil
}

func (dotNetBackend *dotNetBackend) Capacity() (api.Capacity, error) {
	capacity := api.Capacity{}
	return capacity, nil
}

func (dotNetBackend *dotNetBackend) Create(containerSpec api.ContainerSpec) (api.Container, error) {
	netContainer := container.NewContainer(dotNetBackend.TupperwareURL())
	url := dotNetBackend.tupperwareURL + "/api/containers"
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

func (dotNetBackend *dotNetBackend) Destroy(handle string) error {
	url := dotNetBackend.tupperwareURL + "/api/containers/" + handle

	req, err := http.NewRequest("DELETE", url, nil)
	if err != nil {
		return err
	}
	_, err = http.DefaultClient.Do(req)

	return err
}

func (dotNetBackend *dotNetBackend) Containers(api.Properties) ([]api.Container, error) {
	containers := []api.Container{
		container.NewContainer(dotNetBackend.TupperwareURL()),
		container.NewContainer(dotNetBackend.TupperwareURL()),
		container.NewContainer(dotNetBackend.TupperwareURL()),
		container.NewContainer(dotNetBackend.TupperwareURL()),
	}
	return containers, nil
}

func (dotNetBackend *dotNetBackend) Lookup(handle string) (api.Container, error) {
	netContainer := container.NewContainer(dotNetBackend.TupperwareURL())
	return netContainer, nil
}
