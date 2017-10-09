package lifecycle

import (
	"bytes"
	"fmt"
	"os"
	"os/exec"
	"strconv"
	"strings"

	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"

	"code.cloudfoundry.org/garden"
	uuid "github.com/nu7hatch/gouuid"
)

func createDefaultContainer() (garden.Container, error) {
	return createContainer(garden.ContainerSpec{})
}

func createContainer(containerSpec garden.ContainerSpec) (garden.Container, error) {
	handle, err := uuid.NewV4()
	if err != nil {
		return nil, err
	}
	containerSpec.Handle = handle.String()
	container, err := client.Create(containerSpec)
	if err != nil {
		return nil, err
	}
	err = StreamIn(container)
	if err != nil {
		return nil, err
	}
	return container, nil
}

func StreamToDestination(c garden.Container, destPath string) error {
	tarFile, err := os.Open("../bin/consume.tar")
	Expect(err).ShouldNot(HaveOccurred())
	defer tarFile.Close()
	return c.StreamIn(garden.StreamInSpec{Path: destPath, TarStream: tarFile})
}

func StreamIn(c garden.Container) error {
	return StreamToDestination(c, "bin")
}

func AssertMemoryLimits(container garden.Container) {
	buf := make([]byte, 0, 1024*1024)
	stdout := bytes.NewBuffer(buf)

	process, err := container.Run(garden.ProcessSpec{
		Path: "bin/consume.exe",
		Args: []string{"memory", "128"},
	}, garden.ProcessIO{Stdout: stdout})
	Expect(err).ShouldNot(HaveOccurred())

	exitCode, err := process.Wait()
	Expect(err).ShouldNot(HaveOccurred())
	// consume script will exit 42 if it is not killed
	Expect(exitCode).ToNot(Equal(42), "process did not get OOM killed")
	Expect(stdout.String()).To(ContainSubstring("Consumed:  3 mb"))
}

var _ = Describe("Process limits", func() {
	var (
		maxContainerProcs int
		gardenArgs        []string
		container         garden.Container
	)

	BeforeEach(func() {
		gardenArgs = []string{}
		maxContainerProcs = 0
	})

	AfterEach(func() {
		if container != nil {
			err := client.Destroy(container.Handle())
			Expect(err).ShouldNot(HaveOccurred())
		}
	})

	JustBeforeEach(func() {
		client = startGarden(maxContainerProcs, gardenArgs...)
	})

	Context("container process limits", func() {
		testMaxProcs := func(maxProcs int) {
			containerProcs := maxProcs / 2
			var err error
			container, err = createContainer(garden.ContainerSpec{})
			Expect(err).ShouldNot(HaveOccurred())

			launchProc := func() {
				_, err := container.Run(garden.ProcessSpec{
					Path: "C:\\Windows\\System32\\cmd.exe",
				}, garden.ProcessIO{})
				Expect(err).NotTo(HaveOccurred())
			}

			for i := 0; i < containerProcs; i++ {
				go func() {
					defer GinkgoRecover()
					launchProc()
				}()
			}

			output, err := exec.Command("cmd.exe", "/C", "wmic process where (Name='IronFrame.Host.exe') get ProcessId").CombinedOutput()
			Expect(err).NotTo(HaveOccurred())

			containerPid := strings.Fields(string(output))[1]

			getContainerProcCount := func() int {
				output, err = exec.Command("cmd.exe", "/C", "wmic process where (ParentProcessId="+containerPid+") get ProcessId").CombinedOutput()
				Expect(err).NotTo(HaveOccurred())

				return len(strings.Fields(string(output))[1:])
			}

			Eventually(getContainerProcCount, "20s").Should(Equal(containerProcs))
			launchProc()
			Consistently(getContainerProcCount, "2s").Should(Equal(containerProcs))
		}

		Context("when configured with a max number of processes", func() {
			BeforeEach(func() {
				maxContainerProcs = 14
			})

			It("limits the number of active processes to the specified value", func() {
				testMaxProcs(maxContainerProcs)
			})
		})

		Context("when not configured with a max number of container processes", func() {
			It("uses the default as the max number of active processes", func() {
				testMaxProcs(10)
			})
		})
	})

	Describe("a started process", func() {
		Describe("a memory limit", func() {
			It("is enforced when changed at creation", func() {
				var err error
				container, err = createContainer(garden.ContainerSpec{
					Limits: garden.Limits{
						Memory: garden.MemoryLimits{LimitInBytes: 64 * 1024 * 1024},
					},
				})
				Expect(err).ShouldNot(HaveOccurred())
				AssertMemoryLimits(container)
			})

			It("handles large limits", func() {
				var err error
				container, err = createContainer(garden.ContainerSpec{
					Limits: garden.Limits{
						Memory: garden.MemoryLimits{LimitInBytes: 4 * 1024 * 1024 * 1024},
					},
				})
				Expect(err).ShouldNot(HaveOccurred())
			})
		})

		XDescribe("a cpu limit", func() {
			var containers [2]garden.Container

			BeforeEach(func() {
				c1, err := createContainer(garden.ContainerSpec{
					Limits: garden.Limits{
						CPU: garden.CPULimits{LimitInShares: 2500},
					},
				})
				Expect(err).ShouldNot(HaveOccurred())
				c2, err := createContainer(garden.ContainerSpec{
					Limits: garden.Limits{
						CPU: garden.CPULimits{LimitInShares: 7500},
					},
				})
				Expect(err).ShouldNot(HaveOccurred())
				containers[0] = c1
				containers[1] = c2
			})

			AfterEach(func() {
				for _, c := range containers {
					Expect(client.Destroy(c.Handle())).Should(Succeed())
				}
			})

			It("is enforced", func() {
				cpuLimit, err := containers[0].CurrentCPULimits()
				Expect(err).ToNot(HaveOccurred())
				Expect(cpuLimit.LimitInShares).To(Equal(uint64(2500)))

				var processes [2]garden.Process
				var stdouts [2]*bytes.Buffer
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
			It("generated data is enforced", func() {
				limit := 15
				limitInBytes := uint64(limit * 1024 * 1024)
				limitExcludingConsumeBinary := limit - 8

				var err error
				container, err = createContainer(
					garden.ContainerSpec{
						Limits: garden.Limits{
							Disk: garden.DiskLimits{ByteHard: limitInBytes},
						},
					},
				)
				Expect(err).ShouldNot(HaveOccurred())

				buf := make([]byte, 0, 1024*1024)
				stdout := bytes.NewBuffer(buf)

				process, err := container.Run(garden.ProcessSpec{
					Path: "bin/consume.exe",
					Args: []string{"disk", strconv.Itoa(limitExcludingConsumeBinary + 1)},
				}, garden.ProcessIO{Stdout: stdout})
				Expect(err).ShouldNot(HaveOccurred())

				exitCode, err := process.Wait()
				Expect(err).ShouldNot(HaveOccurred())
				Expect(stdout.String()).To(ContainSubstring("Consumed:  3 mb"))
				Expect(stdout.String()).NotTo(ContainSubstring(fmt.Sprintf("Consumed:  %d mb", limitExcludingConsumeBinary+1)))
				Expect(stdout.String()).NotTo(ContainSubstring("Disk Consumed Successfully"))
				Expect(exitCode).NotTo(Equal(42), "process did not get killed")

				limits, err := container.CurrentDiskLimits()
				Expect(err).ShouldNot(HaveOccurred())
				Expect(limits.ByteHard).To(Equal(limitInBytes))
			})

			It("streamed in data is enforced", func() {
				limit := uint64(8 * 1024 * 1024)
				var err error
				container, err = createContainer(
					garden.ContainerSpec{
						Limits: garden.Limits{
							Disk: garden.DiskLimits{ByteHard: limit},
						},
					},
				)
				Expect(err).ShouldNot(HaveOccurred())

				err = StreamToDestination(container, "test")
				Expect(err).Should(HaveOccurred())
				metrics, err := container.Metrics()
				Expect(err).ShouldNot(HaveOccurred())
				Expect(metrics.DiskStat.TotalBytesUsed).Should(BeNumerically("<", limit))
			})
		})
	})
})
