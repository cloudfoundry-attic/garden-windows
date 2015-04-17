package lifecycle_test

import (
	"bytes"
	"os"

	"github.com/cloudfoundry-incubator/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
)

var _ = Describe("Lifecycle", func() {
	var c garden.Container
	var err error

	JustBeforeEach(func() {
		client = startGarden()
		c, err = client.Create(garden.ContainerSpec{})
		Expect(err).ToNot(HaveOccurred())
	})

	AfterEach(func() {
		err := client.Destroy(c.Handle())
		Expect(err).ShouldNot(HaveOccurred())
	})

	Describe("process pid", func() {

		It("returns the pid", func() {
			tarFile, err := os.Open("../bin/consume.tar.gz")
			Expect(err).ShouldNot(HaveOccurred())
			defer tarFile.Close()

			err = c.StreamIn("bin", tarFile)
			Expect(err).ShouldNot(HaveOccurred())

			buf := make([]byte, 0, 1024*1024)
			stdout := bytes.NewBuffer(buf)

			process, err := c.Run(garden.ProcessSpec{
				Path: "bin/consume.exe",
				Args: []string{"64"},
			}, garden.ProcessIO{Stdout: stdout})
			Expect(err).ShouldNot(HaveOccurred())
			// NOTE: we have to cast the pid to uint32, otherwise int(0) !=
			// uint32(0) and the following will be trivially true for any
			// value of the pid
			Expect(process.ID()).ToNot(Equal(uint32(0)))
		})
	})

	Describe("handle collisions", func() {
		It("returns an error", func() {
			_, err := client.Create(garden.ContainerSpec{Handle: c.Handle()})
			Expect(err).To(HaveOccurred())
			Expect(err.Error()).To(Equal("handle already exists: foo"))
		})
	})
})
