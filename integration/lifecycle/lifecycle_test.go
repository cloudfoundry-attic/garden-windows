package lifecycle

import (
	"bytes"
	"os"
	"time"

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

	Describe("process", func() {
		It("pid is returned", func() {
			tarFile, err := os.Open("../bin/consume.tar")
			Expect(err).ShouldNot(HaveOccurred())
			defer tarFile.Close()

			err = c.StreamIn(garden.StreamInSpec{Path: "bin", TarStream: tarFile})
			Expect(err).ShouldNot(HaveOccurred())

			buf := make([]byte, 0, 1024*1024)
			stdout := bytes.NewBuffer(buf)

			process, err := c.Run(garden.ProcessSpec{
				Path: "bin/consume.exe",
				Args: []string{"64"},
			}, garden.ProcessIO{Stdout: stdout})
			Expect(err).ShouldNot(HaveOccurred())
			Expect(process.ID()).ToNot(Equal("0"))
		})

		It("can be signaled", func(done Done) {
			for _, f := range []string{"../bin/loop.tar", "../bin/launcher.tar"} {
				tarFile, err := os.Open(f)
				Expect(err).ShouldNot(HaveOccurred())
				defer tarFile.Close()

				err = c.StreamIn(garden.StreamInSpec{Path: "bin", TarStream: tarFile})
				Expect(err).ShouldNot(HaveOccurred())
			}

			process, err := c.Run(garden.ProcessSpec{
				Path: "bin/launcher.exe",
				Args: []string{"bin/loop.exe"},
			}, garden.ProcessIO{})
			Expect(err).ShouldNot(HaveOccurred())
			go func() {
				// wait for a second and kill the process
				time.Sleep(1 * time.Second)
				err := process.Signal(garden.SignalKill)
				Expect(err).To(Succeed())
			}()
			process.Wait()
			close(done)
		}, 10.0)
	})

	Describe("handle collisions", func() {
		It("returns an error", func() {
			_, err := client.Create(garden.ContainerSpec{Handle: c.Handle()})
			Expect(err).To(HaveOccurred())
			Expect(err.Error()).To(Equal("handle already exists: " + c.Handle()))
		})
	})
})
