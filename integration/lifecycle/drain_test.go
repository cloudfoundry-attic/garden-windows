package lifecycle_test

import (
	"bytes"
	"os"

	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"

	"github.com/cloudfoundry-incubator/garden"
	uuid "github.com/nu7hatch/gouuid"
)

var _ = Describe("Memory limits", func() {
	var container garden.Container
	var gardenArgs []string

	BeforeEach(func() {
		gardenArgs = []string{}
	})

	JustBeforeEach(func() {
		client = startGarden(gardenArgs...)

		var err error

		handle, err := uuid.NewV4()
		Expect(err).ShouldNot(HaveOccurred())
		container, err = client.Create(garden.ContainerSpec{Handle: handle.String()})
		Expect(err).ShouldNot(HaveOccurred())
	})

	AfterEach(func() {
		if container != nil {
			err := client.Destroy(container.Handle())
			Expect(err).ShouldNot(HaveOccurred())
		}
	})

	Describe("a started process", func() {
		Describe("a memory limit", func() {
			It("is enforced", func() {
				tarFile, err := os.Open("../bin/consume.tar.gz")
				Expect(err).ShouldNot(HaveOccurred())
				defer tarFile.Close()

				err = container.StreamIn("bin", tarFile)
				Expect(err).ShouldNot(HaveOccurred())

				err = container.LimitMemory(garden.MemoryLimits{64 * 1024 * 1024})
				Expect(err).ShouldNot(HaveOccurred())

				buf := make([]byte, 0, 1024*1024)
				stdout := bytes.NewBuffer(buf)

				process, err := container.Run(garden.ProcessSpec{
					Path: "bin/consume.exe",
					Args: []string{"128"},
				}, garden.ProcessIO{Stdout: stdout})
				Expect(err).ShouldNot(HaveOccurred())

				exitCode, err := process.Wait()
				Expect(err).ShouldNot(HaveOccurred())
				Expect(exitCode).ToNot(Equal(42), "process did not get OOM killed")
				Expect(stdout).To(ContainSubstring("Consumed:  3 mb"))
			})
		})
	})
})
