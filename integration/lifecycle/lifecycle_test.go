package lifecycle_test

import (
	"github.com/cloudfoundry-incubator/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
)

var _ = Describe("Lifecycle", func() {
	Describe("handle collisions", func() {
		var c garden.Container

		AfterEach(func() {
			err := client.Destroy(c.Handle())
			Expect(err).ShouldNot(HaveOccurred())
		})
		It("returns an error", func() {
			var err error
			client = startGarden()
			c, err = client.Create(garden.ContainerSpec{})
			Expect(err).NotTo(HaveOccurred())
			_, err = client.Create(garden.ContainerSpec{Handle: c.Handle()})
			Expect(err).To(HaveOccurred())
			Expect(err.Error()).To(Equal("handle already exists: foo"))
		})
	})
})
