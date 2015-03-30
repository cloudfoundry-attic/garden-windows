package containerizer_url

import (
	"errors"
	url "net/url"
)

type ContainerizerURL struct {
	base *url.URL
}

func FromString(hi string) (*ContainerizerURL, error) {
	url, err := url.Parse(hi)
	if err != nil {
		return nil, err
	}
	if !url.IsAbs() {
		return nil, errors.New("Containerizer url must be absolute.")
	}
	url.Scheme = "http"
	return &ContainerizerURL{base: url}, err
}

func (self ContainerizerURL) Ping() string {
	base := *self.base
	base.Path = "/api/ping"
	return base.String()
}

func (self ContainerizerURL) Create() string {
	base := *self.base
	base.Path = "/api/containers"
	return base.String()
}

func (self ContainerizerURL) Destroy(handle string) string {
	base := *self.base
	base.Path = "/api/containers/" + handle
	return base.String()
}

func (self ContainerizerURL) List() string {
	base := *self.base
	base.Path = "/api/containers"
	return base.String()
}

func (self ContainerizerURL) BulkInfo() string {
	base := *self.base
	base.Path = "/api/bulkcontainerinfo"
	return base.String()
}

func (self ContainerizerURL) Stop(handle string) string {
	base := *self.base
	base.Path = "/api/containers/" + handle + "/stop"
	return base.String()
}

func (self ContainerizerURL) GetProperties(handle string) string {
	base := *self.base
	base.Path = "/api/containers/" + handle + "/properties"
	return base.String()
}

func (self ContainerizerURL) Info(handle string) string {
	base := *self.base
	base.Path = "/api/containers/" + handle + "/info"
	return base.String()
}

func (self ContainerizerURL) StreamIn(handle string, destination string) string {
	base := *self.base
	base.Path = "/api/containers/" + handle + "/files"
	q := base.Query()
	q.Add("destination", destination)
	base.RawQuery = q.Encode()
	return base.String()
}

func (self ContainerizerURL) StreamOut(handle string, source string) string {
	base := *self.base
	base.Path = "/api/containers/" + handle + "/files"
	q := base.Query()
	q.Add("source", source)
	base.RawQuery = q.Encode()
	return base.String()
}

func (self ContainerizerURL) NetIn(handle string) string {
	base := *self.base
	base.Path = "/api/containers/" + handle + "/net/in"
	return base.String()
}

func (self ContainerizerURL) Run(handle string) string {
	base := *self.base
	base.Scheme = "ws"
	base.Path = "api/containers/" + handle + "/run"
	return base.String()
}

func (self ContainerizerURL) GetProperty(handle string, property string) string {
	base := *self.base
	base.Path = "/api/containers/" + handle + "/properties/" + property
	return base.String()
}

func (self ContainerizerURL) SetProperty(handle string, property string) string {
	base := *self.base
	base.Path = "/api/containers/" + handle + "/properties/" + property
	return base.String()
}

func (self ContainerizerURL) RemoveProperty(handle string, property string) string {
	base := *self.base
	base.Path = "/api/containers/" + handle + "/properties/" + property
	return base.String()
}
