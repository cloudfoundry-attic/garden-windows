package backend

import (
	"encoding/json"
	"net/http"
	"net/url"
	"time"

	"strings"

	"github.com/cloudfoundry-incubator/garden/api"
	"github.com/pivotal-cf-experimental/garden-dot-net/container"
)

type dotNetBackend struct {
	tupperwareURL url.URL
}

func NewDotNetBackend(tupperwareURL string) (*dotNetBackend, error) {
	u, err := url.Parse(tupperwareURL)
	if err != nil {
		return nil, err
	}
	return &dotNetBackend{
		tupperwareURL: *u,
	}, nil
}

func (dotNetBackend *dotNetBackend) TupperwareURL() string {
	return dotNetBackend.tupperwareURL.String()
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
	netContainer := container.NewContainer(dotNetBackend.tupperwareURL, "containerhandle")
	url := dotNetBackend.tupperwareURL.String() + "/api/containers"
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
	url := dotNetBackend.tupperwareURL.String() + "/api/containers/" + handle

	req, err := http.NewRequest("DELETE", url, nil)
	if err != nil {
		return err
	}
	_, err = http.DefaultClient.Do(req)

	return err
}

func (dotNetBackend *dotNetBackend) Containers(api.Properties) ([]api.Container, error) {
	containers := []api.Container{
		container.NewContainer(dotNetBackend.tupperwareURL, "containerhandle"),
		container.NewContainer(dotNetBackend.tupperwareURL, "containerhandle"),
		container.NewContainer(dotNetBackend.tupperwareURL, "containerhandle"),
		container.NewContainer(dotNetBackend.tupperwareURL, "containerhandle"),
	}
	return containers, nil
}

func (dotNetBackend *dotNetBackend) Lookup(handle string) (api.Container, error) {
	netContainer := container.NewContainer(dotNetBackend.tupperwareURL, handle)
	return netContainer, nil
}
