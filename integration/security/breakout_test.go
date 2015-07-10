package security

import (
	"fmt"
	"os"

	"github.com/mitchellh/go-ps"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"

	"github.com/cloudfoundry-incubator/garden"
	uuid "github.com/nu7hatch/gouuid"
)

func createContainerWithoutConsume() garden.Container {
	handle, err := uuid.NewV4()
	Expect(err).ShouldNot(HaveOccurred())
	container, err := client.Create(garden.ContainerSpec{Handle: handle.String()})
	Expect(err).ShouldNot(HaveOccurred())
	return container
}

func createContainer() garden.Container {
	c := createContainerWithoutConsume()
	err := StreamIn(c)
	Expect(err).ShouldNot(HaveOccurred())
	return c
}

func StreamIn(c garden.Container) error {
	tarFile, err := os.Open("../../greenhouse-security-fixtures/output/SecurityFixtures.tgz")
	Expect(err).ShouldNot(HaveOccurred())
	defer tarFile.Close()
	return c.StreamIn(garden.StreamInSpec{Path: "bin", TarStream: tarFile})
}

var _ = XDescribe("Breakout", func() {
	Describe("a started process", func() {
		Describe("breaking out", func() {
			var (
				container   garden.Container
				pingProcess ps.Process
			)
			BeforeEach(func() {
				container = createContainer()
			})
			AfterEach(func() {
				if pingProcess == nil {
					return
				}
				process, err := os.FindProcess(pingProcess.Pid())
				if err == nil {
					process.Kill()
				}
			})

			It("is not allowed", func() {
				// TODO: make the executable name unique so to avoid test pollution
				_, err := container.Run(garden.ProcessSpec{
					Path: "bin/JobBreakoutTest.exe",
					Args: []string{"ping 192.0.2.2 -n 1 -w 10000"},
				}, garden.ProcessIO{})
				Expect(err).ShouldNot(HaveOccurred())

				err = client.Destroy(container.Handle())
				Expect(err).ShouldNot(HaveOccurred())

				processes, err := ps.Processes()
				Expect(err).ShouldNot(HaveOccurred())
				for _, proc := range processes {
					fmt.Println(proc.Executable())
					if proc.Executable() == "PING.EXE" {
						pingProcess = proc
						Expect(proc.Executable()).NotTo(Equal("PING.EXE"))
					}
				}
			})
		})

	})
})
