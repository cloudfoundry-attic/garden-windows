package lifecycle

import (
	"fmt"
	"net"
	"os"
	"time"

	"github.com/cloudfoundry-incubator/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
)

var _ = Describe("NetOut", func() {
	var c garden.Container
	var err error

	JustBeforeEach(func() {
		client = startGarden()
		c, err = client.Create(garden.ContainerSpec{})
		Expect(err).ToNot(HaveOccurred())

		tarFile, err := os.Open("../bin/connect_to_remote_url.tgz")
		Expect(err).ShouldNot(HaveOccurred())
		defer tarFile.Close()
		err = c.StreamIn("bin", tarFile)
		Expect(err).ShouldNot(HaveOccurred())
	})

	AfterEach(func() {
		err := client.Destroy(c.Handle())
		Expect(err).ShouldNot(HaveOccurred())
	})

	openPort := func(proto garden.Protocol, port uint16, ip string) {
		rule := garden.NetOutRule{
			Protocol: proto,
		}
		if ip != "" {
			parsedIP := net.ParseIP(ip)
			Expect(parsedIP).ToNot(BeNil())
			rule.Networks = []garden.IPRange{
				{
					Start: parsedIP,
					End:   parsedIP,
				},
			}
		}

		if port > 0 {
			rule.Ports = []garden.PortRange{
				{
					Start: port,
					End:   port,
				},
			}
		}
		Expect(c.NetOut(rule)).To(Succeed())
	}

	Describe("netout", func() {
		Describe("when the All protocol is used", func() {
			It("allow both tcp and udp connections", func() {
				var dnsAndHttpPort uint16 = 53

				openPort(garden.ProtocolAll, dnsAndHttpPort, "")

				process, err := c.Run(garden.ProcessSpec{
					Path: "bin/connect_to_remote_url.exe",
					Env:  []string{fmt.Sprintf("URL=%v", "http://portquiz.net:53")},
				}, garden.ProcessIO{})
				Expect(err).ShouldNot(HaveOccurred())
				exitCode, err := process.Wait()
				Expect(err).ShouldNot(HaveOccurred())
				Expect(exitCode).To(Equal(0))
			})
		})

		It("propogates errors", func() {
			err := c.NetOut(garden.NetOutRule{
				Protocol: garden.ProtocolTCP,
				Networks: []garden.IPRange{
					{
						Start: net.ParseIP("1.2.3.4"),
						End:   net.ParseIP("1.2.3.1"),
					},
				},
			})
			Expect(err).To(HaveOccurred())
		})
	})

	Describe("outbound tcp traffic", func() {
		hostaddr := "178.33.250.62"
		var port uint16 = 9090
		httpUrl := fmt.Sprintf("http://%s:%d", hostaddr, port)

		It("is disabled by default", func() {
			process, err := c.Run(garden.ProcessSpec{
				Path: "bin/connect_to_remote_url.exe",
				Env:  []string{fmt.Sprintf("URL=%v", httpUrl)},
			}, garden.ProcessIO{})
			Expect(err).ShouldNot(HaveOccurred())
			exitCode, err := process.Wait()
			Expect(err).ShouldNot(HaveOccurred())
			Expect(exitCode).ToNot(Equal(0))
		})

		It("can be allowed by whitelisting ip addresses", func() {
			openPort(garden.ProtocolTCP, 0, hostaddr)

			process, err := c.Run(garden.ProcessSpec{
				Path: "bin/connect_to_remote_url.exe",
				Env:  []string{fmt.Sprintf("URL=%v", httpUrl)},
			}, garden.ProcessIO{})
			Expect(err).ShouldNot(HaveOccurred())
			exitCode, err := process.Wait()
			Expect(err).ShouldNot(HaveOccurred())
			Expect(exitCode).To(Equal(0))
		})

		It("can be allowed by whitelisting ports", func() {
			openPort(garden.ProtocolTCP, port, "")

			process, err := c.Run(garden.ProcessSpec{
				Path: "bin/connect_to_remote_url.exe",
				Env:  []string{fmt.Sprintf("URL=%v", httpUrl)},
			}, garden.ProcessIO{})
			Expect(err).ShouldNot(HaveOccurred())
			exitCode, err := process.Wait()
			Expect(err).ShouldNot(HaveOccurred())
			Expect(exitCode).To(Equal(0))
		})

		It("can be allowed by whitelisting both ip and port", func() {
			openPort(garden.ProtocolTCP, port, hostaddr)

			process, err := c.Run(garden.ProcessSpec{
				Path: "bin/connect_to_remote_url.exe",
				Env:  []string{fmt.Sprintf("URL=%v", httpUrl)},
			}, garden.ProcessIO{})
			Expect(err).ShouldNot(HaveOccurred())
			exitCode, err := process.Wait()
			Expect(err).ShouldNot(HaveOccurred())
			Expect(exitCode).To(Equal(0))
		})
	})

	Describe("outbound udp traffic", func() {
		FIt("can be allowed by whitelisting udp ports", func() {
			openPort(garden.ProtocolUDP, 53, "")
			openPort(garden.ProtocolTCP, 9090, "")

			process, err := c.Run(garden.ProcessSpec{
				Path: "bin/connect_to_remote_url.exe",
				Env:  []string{fmt.Sprintf("URL=%v", "http://portquiz.net:9090")},
			}, garden.ProcessIO{})
			Expect(err).ShouldNot(HaveOccurred())
			exitCode, err := process.Wait()
			Expect(err).ShouldNot(HaveOccurred())
			Expect(exitCode).To(Equal(0))
			fmt.Println("Sleeping for 60 minutes")
			time.Sleep(60 * time.Minute)
		})
	})
})
