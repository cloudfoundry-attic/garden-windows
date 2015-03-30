package backend

import (
	"encoding/json"
	"io/ioutil"
	"net/http"
	"time"

	"strings"

	"github.com/cloudfoundry-incubator/garden"
	"github.com/cloudfoundry-incubator/garden-windows/container"
	"github.com/cloudfoundry-incubator/garden-windows/containerizer_url"
	"github.com/pivotal-golang/lager"
)

type dotNetBackend struct {
	containerizerURL *containerizer_url.ContainerizerURL
	logger           lager.Logger
}

func NewDotNetBackend(containerizerURL *containerizer_url.ContainerizerURL, logger lager.Logger) (*dotNetBackend, error) {
	return &dotNetBackend{
		containerizerURL: containerizerURL,
		logger:           logger,
	}, nil
}

func (dotNetBackend *dotNetBackend) Start() error {
	return nil
}

func (dotNetBackend *dotNetBackend) Stop() {}

func (dotNetBackend *dotNetBackend) GraceTime(garden.Container) time.Duration {
	// FIXME -- what should this do.
	return time.Hour
}

func (dotNetBackend *dotNetBackend) Ping() error {
	resp, err := http.Get(dotNetBackend.containerizerURL.Ping())
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
	url := dotNetBackend.containerizerURL.Create()
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
	url := dotNetBackend.containerizerURL.Destroy(handle)

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
	url := dotNetBackend.containerizerURL.List()
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

func (dotNetBackend *dotNetBackend) BulkInfo(handles []string) (map[string]garden.ContainerInfoEntry, error) {
	url := dotNetBackend.containerizerURL.BulkInfo()
	containerSpecJSON, err := json.Marshal(handles)
	if err != nil {
		return nil, err
	}
	resp, err := http.Post(url, "application/json", strings.NewReader(string(containerSpecJSON)))
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	containersInfo := make(map[string]garden.ContainerInfoEntry)
	err = json.NewDecoder(resp.Body).Decode(&containersInfo)
	return containersInfo, err
}

func (dotNetBackend *dotNetBackend) BulkMetrics(handles []string) (map[string]garden.ContainerMetricsEntry, error) {
	return nil, nil
}
