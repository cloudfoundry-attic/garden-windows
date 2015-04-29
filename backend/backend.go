package backend

import (
	"time"

	"github.com/cloudfoundry-incubator/garden"
	"github.com/cloudfoundry-incubator/garden-windows/container"
	"github.com/cloudfoundry-incubator/garden-windows/containerizer_url"
	"github.com/cloudfoundry-incubator/garden-windows/http_client"
	"github.com/pivotal-golang/lager"
)

type dotNetBackend struct {
	containerizerURL *containerizer_url.ContainerizerURL
	logger           lager.Logger
	client           *http_client.Client
}

func NewDotNetBackend(containerizerURL *containerizer_url.ContainerizerURL, logger lager.Logger) (*dotNetBackend, error) {
	return &dotNetBackend{
		containerizerURL: containerizerURL,
		logger:           logger,
		client:           http_client.NewClient(logger, containerizerURL.Base()),
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
	return dotNetBackend.client.Get(dotNetBackend.containerizerURL.Ping(), nil)
}

func (dotNetBackend *dotNetBackend) Capacity() (garden.Capacity, error) {
	var capacity garden.Capacity
	err := dotNetBackend.client.Get(dotNetBackend.containerizerURL.Capacity(), &capacity)
	return capacity, err
}

func (dotNetBackend *dotNetBackend) Create(containerSpec garden.ContainerSpec) (garden.Container, error) {
	url := dotNetBackend.containerizerURL.Create()
	var returnedContainer createContainerResponse
	err := dotNetBackend.client.Post(url, containerSpec, &returnedContainer)
	netContainer := container.NewContainer(dotNetBackend.containerizerURL, returnedContainer.Handle, dotNetBackend.logger)
	return netContainer, err
}

func (dotNetBackend *dotNetBackend) Destroy(handle string) error {
	url := dotNetBackend.containerizerURL.Destroy(handle)
	return dotNetBackend.client.Delete(url)
}

func (dotNetBackend *dotNetBackend) Containers(props garden.Properties) ([]garden.Container, error) {
	url, err := dotNetBackend.containerizerURL.List(props)
	if err != nil {
		return nil, err
	}
	var ids []string
	err = dotNetBackend.client.Get(url, &ids)
	containers := []garden.Container{}
	for _, containerId := range ids {
		containers = append(containers, container.NewContainer(dotNetBackend.containerizerURL, containerId, dotNetBackend.logger))
	}
	return containers, err
}

func (dotNetBackend *dotNetBackend) Lookup(handle string) (garden.Container, error) {
	netContainer := container.NewContainer(dotNetBackend.containerizerURL, handle, dotNetBackend.logger)
	return netContainer, nil
}

func (dotNetBackend *dotNetBackend) BulkInfo(handles []string) (map[string]garden.ContainerInfoEntry, error) {
	url := dotNetBackend.containerizerURL.BulkInfo()
	containersInfo := make(map[string]garden.ContainerInfoEntry)
	err := dotNetBackend.client.Post(url, handles, &containersInfo)
	return containersInfo, err
}

func (dotNetBackend *dotNetBackend) BulkMetrics(handles []string) (map[string]garden.ContainerMetricsEntry, error) {
	return nil, nil
}
