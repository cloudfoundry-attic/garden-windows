package backend

import (
	"encoding/json"
	"io/ioutil"
	"net/http"
	"net/url"
	"time"

	"strings"

	"github.com/cloudfoundry-incubator/garden/api"
	"github.com/pivotal-cf-experimental/garden-dot-net/container"
)

type dotNetBackend struct {
	containerizerURL url.URL
}

func NewDotNetBackend(containerizerURL string) (*dotNetBackend, error) {
	u, err := url.Parse(containerizerURL)
	if err != nil {
		return nil, err
	}
	return &dotNetBackend{
		containerizerURL: *u,
	}, nil
}

func (dotNetBackend *dotNetBackend) ContainerizerURL() string {
	return dotNetBackend.containerizerURL.String()
}

func (dotNetBackend *dotNetBackend) Start() error {
	return nil
}

func (dotNetBackend *dotNetBackend) Stop() {}

func (dotNetBackend *dotNetBackend) GraceTime(api.Container) time.Duration {
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

func (dotNetBackend *dotNetBackend) Capacity() (api.Capacity, error) {
	capacity := api.Capacity{
		MemoryInBytes: 8 * 1024 * 1024 * 1024,
		DiskInBytes:   80 * 1024 * 1024 * 1024,
		MaxContainers: 100,
	}
	return capacity, nil
}

func (dotNetBackend *dotNetBackend) Create(containerSpec api.ContainerSpec) (api.Container, error) {
	url := dotNetBackend.containerizerURL.String() + "/api/containers"
	containerSpecJSON, err := json.Marshal(containerSpec)
	if err != nil {
		return nil, err
	}
	_, err = http.Post(url, "application/json", strings.NewReader(string(containerSpecJSON)))
	if err != nil {
		return nil, err
	}

	netContainer := container.NewContainer(dotNetBackend.containerizerURL, containerSpec.Handle)
	return netContainer, nil
}

func (dotNetBackend *dotNetBackend) Destroy(handle string) error {
	url := dotNetBackend.containerizerURL.String() + "/api/containers/" + handle

	req, err := http.NewRequest("DELETE", url, nil)
	if err != nil {
		return err
	}
	_, err = http.DefaultClient.Do(req)

	return err
}

func (dotNetBackend *dotNetBackend) Containers(api.Properties) ([]api.Container, error) {
	url := dotNetBackend.containerizerURL.String() + "/api/containers"
	response, err := http.Get(url)
	if err != nil {
		return nil, err
	}

	var ids []string
	body, err := ioutil.ReadAll(response.Body)
	if err != nil {
		return nil, err
	}
	err = json.Unmarshal(body, &ids)

	containers := []api.Container{}
	for _, containerId := range ids {
		containers = append(containers, container.NewContainer(dotNetBackend.containerizerURL, containerId))
	}
	return containers, nil
}

func (dotNetBackend *dotNetBackend) Lookup(handle string) (api.Container, error) {
	netContainer := container.NewContainer(dotNetBackend.containerizerURL, handle)
	return netContainer, nil
}
