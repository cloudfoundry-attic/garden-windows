package lifecycle

import (
	"bytes"
	"os"
	"strconv"
	"strings"

	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"

	"github.com/cloudfoundry-incubator/garden"
	uuid "github.com/nu7hatch/gouuid"
)

func createContainer() garden.Container {
	handle, err := uuid.NewV4()
	Expect(err).ShouldNot(HaveOccurred())
	container, err := client.Create(garden.ContainerSpec{Handle: handle.String()})
	Expect(err).ShouldNot(HaveOccurred())
	tarFile, err := os.Open("../bin/consume.tgz")
	Expect(err).ShouldNot(HaveOccurred())
	defer tarFile.Close()
	err = container.StreamIn("bin", tarFile)
	Expect(err).ShouldNot(HaveOccurred())
	return container
}

var _ = Describe("Process limits", func() {
	var gardenArgs []string

	BeforeEach(func() {
		gardenArgs = []string{}
		client = startGarden(gardenArgs...)
	})

	Describe("a started process", func() {
		Describe("a memory limit", func() {
			var container garden.Container
			BeforeEach(func() {
				container = createContainer()
			})

			AfterEach(func() {
				err := client.Destroy(container.Handle())
				Expect(err).ShouldNot(HaveOccurred())
			})

			It("is enforced", func() {
				err := container.LimitMemory(garden.MemoryLimits{64 * 1024 * 1024})
				Expect(err).ShouldNot(HaveOccurred())

				buf := make([]byte, 0, 1024*1024)
				stdout := bytes.NewBuffer(buf)

				process, err := container.Run(garden.ProcessSpec{
					Path: "bin/consume.exe",
					Args: []string{"memory", "128"},
				}, garden.ProcessIO{Stdout: stdout})
				Expect(err).ShouldNot(HaveOccurred())

				exitCode, err := process.Wait()
				Expect(err).ShouldNot(HaveOccurred())
				Expect(exitCode).ToNot(Equal(42), "process did not get OOM killed")
				Expect(stdout.String()).To(ContainSubstring("Consumed:  3 mb"))
			})
		})

		Describe("a cpu limit", func() {
			var containers [2]garden.Container

			BeforeEach(func() {
				containers[0] = createContainer()
				containers[1] = createContainer()
			})

			AfterEach(func() {
				for _, c := range containers {
					Expect(client.Destroy(c.Handle())).Should(Succeed())
				}
			})

			It("is enforced", func() {
				setCPU := func(container garden.Container, limit uint64) error {
					return container.LimitCPU(garden.CPULimits{LimitInShares: limit})
				}
				Expect(setCPU(containers[0], 2500)).Should(Succeed())
				Expect(setCPU(containers[1], 7500)).Should(Succeed())

				var processes [2]garden.Process
				var stdouts [2]*bytes.Buffer
				var err error
				for i, c := range containers {
					buf := make([]byte, 0, 1024*1024)
					stdout := bytes.NewBuffer(buf)
					stdouts[i] = stdout
					processes[i], err = c.Run(garden.ProcessSpec{
						Path: "bin/consume.exe",
						Args: []string{"fork", "cpu", "10s"},
					}, garden.ProcessIO{Stdout: stdout})
					Expect(err).ShouldNot(HaveOccurred())
				}

				for _, p := range processes {
					exitCode, err := p.Wait()
					Expect(err).ToNot(HaveOccurred())
					Expect(exitCode).To(Equal(0))
				}

				var userTimes [2]int
				for i, s := range stdouts {
					stdout := strings.TrimSpace(s.String())
					userTime := strings.Split(stdout, ",")[1]
					userTimes[i], err = strconv.Atoi(strings.Split(userTime, ": ")[1])
					Expect(err).ToNot(HaveOccurred())
				}
				ratio := float64(userTimes[0]) / float64(userTimes[1])
				Expect(ratio).To(BeNumerically("~", 0.25, 0.1))
			})

			It("forkbombs are killed", func(done Done) {
				buf := make([]byte, 0, 1024*1024)
				stdout := bytes.NewBuffer(buf)
				p, err := containers[0].Run(garden.ProcessSpec{
					Path: "bin/consume.exe",
					Args: []string{"forkbomb"},
				}, garden.ProcessIO{Stdout: stdout})
				Expect(err).ShouldNot(HaveOccurred())

				exitCode, err := p.Wait()
				Expect(err).ToNot(HaveOccurred())
				Expect(exitCode).ToNot(Equal(0))
				close(done)
			}, 10.0)
		})

		Describe("disk limits", func() {
			var container garden.Container
			BeforeEach(func() {
				container = createContainer()
			})

			AfterEach(func() {
				err := client.Destroy(container.Handle())
				Expect(err).ShouldNot(HaveOccurred())
			})

			FIt("generated data is enforced", func() {
				err := container.LimitDisk(garden.DiskLimits{ByteHard: 5 * 1024 * 1024})
				Expect(err).ShouldNot(HaveOccurred())

				buf := make([]byte, 0, 1024*1024)
				stdout := bytes.NewBuffer(buf)

				process, err := container.Run(garden.ProcessSpec{
					Path: "bin/consume.exe",
					Args: []string{"disk", "10"},
				}, garden.ProcessIO{Stdout: stdout})
				Expect(err).ShouldNot(HaveOccurred())

				exitCode, err := process.Wait()
				Expect(err).ShouldNot(HaveOccurred())
				Expect(stdout.String()).To(ContainSubstring("Consumed:  3 mb"))
				Expect(stdout.String()).NotTo(ContainSubstring("Consumed:  6 mb"))
				Expect(stdout.String()).NotTo(ContainSubstring("Disk Consumed Successfully"))
				Expect(exitCode).NotTo(Equal(42), "process did not get killed")
			})

			XIt("streamed in data is enforced", func() {

			})
		})
	})
})
