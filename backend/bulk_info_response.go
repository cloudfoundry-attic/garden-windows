package backend

import "github.com/cloudfoundry-incubator/garden"

type BulkInfoError struct {
	Message string
}

func (e BulkInfoError) Error() string {
	return e.Message
}

type BulkInfoResponse struct {
	Info garden.ContainerInfo
	Err  BulkInfoError
}
