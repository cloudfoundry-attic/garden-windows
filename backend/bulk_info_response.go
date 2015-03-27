package backend

import "github.com/cloudfoundry-incubator/garden"

type BulkInfoResponse struct {
	Info garden.ContainerInfo
	Err  string
}
