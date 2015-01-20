package backend

import (
	"encoding/json"
	"io/ioutil"
	"net/http"
	"net/url"
	"time"

	"strings"

	"github.com/cloudfoundry-incubator/garden"
	"github.com/pivotal-cf-experimental/garden-dot-net/container"
	"github.com/pivotal-golang/lager"
)

type dotNetBackend struct {
	containerizerURL url.URL
	logger           lager.Logger
}

func NewDotNetBackend(containerizerURL string, logger lager.Logger) (*dotNetBackend, error) {
	u, err := url.Parse(containerizerURL)
	if err != nil {
		return nil, err
	}
	return &dotNetBackend{
		containerizerURL: *u,
		logger:           logger,
	}, nil
}

func (dotNetBackend *dotNetBackend) ContainerizerURL() string {
	return dotNetBackend.containerizerURL.String()
}

func (dotNetBackend *dotNetBackend) Start() error {
	return nil
}

func (dotNetBackend *dotNetBackend) Stop() {}

func (dotNetBackend *dotNetBackend) GraceTime(garden.Container) time.Duration {
	return time.Second
}

func (dotNetBackend *dotNetBackend) Ping() error {
	resp, err := http.Get(dotNetBackend.containerizerURL.String() + "/api/ping")
	if err != nil {
		return err
	}
	resp.Body.Close()
	return nil
}

func (dotNetBackend *dotNetBackend) Capacity() (garden.Capacity, error) {
	capacity := garden.Capacity{
		MemoryInBytes: 8 * 1024 * 1024 * 1024,
		DiskInBytes:   80 * 1024 * 1024 * 1024,
		MaxContainers: 100,
	}
	return capacity, nil
}

func (dotNetBackend *dotNetBackend) Create(containerSpec garden.ContainerSpec) (garden.Container, error) {
	url := dotNetBackend.containerizerURL.String() + "/api/containers"
	containerSpecJSON, err := json.Marshal(containerSpec)
	if err != nil {
		return nil, err
	}
	resp, err := http.Post(url, "application/json", strings.NewReader(string(containerSpecJSON)))
	if err != nil {
		return nil, err
	}
	resp.Body.Close()

	netContainer := container.NewContainer(dotNetBackend.containerizerURL, containerSpec.Handle, dotNetBackend.logger)
	return netContainer, nil
}

func (dotNetBackend *dotNetBackend) Destroy(handle string) error {
	url := dotNetBackend.containerizerURL.String() + "/api/containers/" + handle

	req, err := http.NewRequest("DELETE", url, nil)
	if err != nil {
		return err
	}

	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		return err
	}
	resp.Body.Close()

	return nil
}

func (dotNetBackend *dotNetBackend) Containers(garden.Properties) ([]garden.Container, error) {
	url := dotNetBackend.containerizerURL.String() + "/api/containers"
	response, err := http.Get(url)
	if err != nil {
		return nil, err
	}
	defer response.Body.Close()

	var ids []string
	body, err := ioutil.ReadAll(response.Body)
	if err != nil {
		return nil, err
	}
	err = json.Unmarshal(body, &ids)

	containers := []garden.Container{}
	for _, containerId := range ids {
		containers = append(containers, container.NewContainer(dotNetBackend.containerizerURL, containerId, dotNetBackend.logger))
	}
	return containers, nil
}

func (dotNetBackend *dotNetBackend) Lookup(handle string) (garden.Container, error) {
	netContainer := container.NewContainer(dotNetBackend.containerizerURL, handle, dotNetBackend.logger)
	return netContainer, nil
}
