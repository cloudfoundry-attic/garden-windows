package backend

import (
	"encoding/json"
	"fmt"
	"net/url"
	"time"

	"github.com/cloudfoundry-incubator/garden"
	"github.com/cloudfoundry-incubator/garden-windows/container"
	"github.com/cloudfoundry-incubator/garden-windows/dotnet"
	"github.com/pivotal-golang/lager"
)

type dotNetBackend struct {
	logger lager.Logger
	client *dotnet.Client
}

func NewDotNetBackend(client *dotnet.Client, logger lager.Logger) (*dotNetBackend, error) {
	return &dotNetBackend{
		logger: logger,
		client: client,
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
	return dotNetBackend.client.Get("/api/ping", nil)
}

func (dotNetBackend *dotNetBackend) Capacity() (garden.Capacity, error) {
	var capacity garden.Capacity
	err := dotNetBackend.client.Get("/api/capacity", &capacity)
	return capacity, err
}

func (dotNetBackend *dotNetBackend) Create(containerSpec garden.ContainerSpec) (garden.Container, error) {
	var returnedContainer createContainerResponse
	err := dotNetBackend.client.Post("/api/containers", containerSpec, &returnedContainer)
	netContainer := container.NewContainer(dotNetBackend.client, returnedContainer.Handle, dotNetBackend.logger)
	return netContainer, err
}

func (dotNetBackend *dotNetBackend) Destroy(handle string) error {
	u := fmt.Sprintf("/api/containers/%s", handle)
	return dotNetBackend.client.Delete(u)
}

func (dotNetBackend *dotNetBackend) Containers(props garden.Properties) ([]garden.Container, error) {
	containers := []garden.Container{}
	u, err := url.Parse("/api/containers")
	if len(props) > 0 {
		jsonString, err := json.Marshal(props)
		if err != nil {
			return containers, err
		}
		values := url.Values{"q": []string{string(jsonString)}}
		u.RawQuery = values.Encode()
	}

	var ids []string
	err = dotNetBackend.client.Get(u.String(), &ids)
	for _, containerId := range ids {
		containers = append(containers, container.NewContainer(dotNetBackend.client, containerId, dotNetBackend.logger))
	}
	return containers, err
}

func (dotNetBackend *dotNetBackend) Lookup(handle string) (garden.Container, error) {
	netContainer := container.NewContainer(dotNetBackend.client, handle, dotNetBackend.logger)
	return netContainer, nil
}

func (dotNetBackend *dotNetBackend) BulkInfo(handles []string) (map[string]garden.ContainerInfoEntry, error) {
	containersInfo := make(map[string]garden.ContainerInfoEntry)
	err := dotNetBackend.client.Post("/api/bulkcontainerinfo", handles, &containersInfo)
	return containersInfo, err
}

func (dotNetBackend *dotNetBackend) BulkMetrics(handles []string) (map[string]garden.ContainerMetricsEntry, error) {
	containersMetrics := make(map[string]garden.ContainerMetricsEntry)
	err := dotNetBackend.client.Post("/api/bulkcontainermetrics", handles, &containersMetrics)
	return containersMetrics, err
}
