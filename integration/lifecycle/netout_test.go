package lifecycle

import (
	"fmt"
	"net"
	"os"

	"github.com/cloudfoundry-incubator/garden"
	"github.com/cloudfoundry/garden-windows/integration/helpers"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
)

var _ = Describe("NetOut", func() {
	var c garden.Container
	var err error
	var udpPort uint16 = 53
	var tcpPort uint16 = 80
	googleIPAddress := "173.194.207.101"
	googleDNSServer := "8.8.8.8"

	JustBeforeEach(func() {
		client = startGarden()
		c, err = client.Create(garden.ContainerSpec{})
		Expect(err).ToNot(HaveOccurred())

		tarFile, err := os.Open("../bin/connect_to_remote_url.tar")
		Expect(err).ShouldNot(HaveOccurred())
		defer tarFile.Close()
		err = c.StreamIn(garden.StreamInSpec{Path: "bin", TarStream: tarFile})
		Expect(err).ShouldNot(HaveOccurred())
	})

	AfterEach(func() {
		err := client.Destroy(c.Handle())
		Expect(err).ShouldNot(HaveOccurred())
	})

	testConnection := func(protocol, address string, port uint16) (garden.Process, error) {
		return c.Run(garden.ProcessSpec{
			Path: "bin/connect_to_remote_url.exe",
			Env: []string{
				fmt.Sprintf("PROTOCOL=%v", protocol),
				fmt.Sprintf("ADDRESS=%v:%v", address, port),
			},
		}, garden.ProcessIO{})
	}

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

				helpers.AssertEventuallyProcessExitsWith(1, func() (garden.Process, error) {
					return testConnection("tcp", googleIPAddress, tcpPort)
				})
				openPort(garden.ProtocolAll, tcpPort, "")
				helpers.AssertEventuallyProcessExitsWith(0, func() (garden.Process, error) {
					return testConnection("tcp", googleIPAddress, tcpPort)
				})

				helpers.AssertEventuallyProcessExitsWith(1, func() (garden.Process, error) {
					return testConnection("udp", googleDNSServer, udpPort)
				})
				openPort(garden.ProtocolAll, udpPort, "")
				helpers.AssertEventuallyProcessExitsWith(0, func() (garden.Process, error) {
					return testConnection("udp", googleDNSServer, udpPort)
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
			blockedGoogleIPAddress := "173.194.207.100"

			It("is disabled by default", func() {
				helpers.AssertEventuallyProcessExitsWith(1, func() (garden.Process, error) {
					return testConnection("tcp", googleIPAddress, tcpPort)
				})
			})

			It("can be allowed by whitelisting ip addresses", func() {
				helpers.AssertEventuallyProcessExitsWith(1, func() (garden.Process, error) {
					return testConnection("tcp", googleIPAddress, tcpPort)
				})

				openPort(garden.ProtocolTCP, 0, googleIPAddress)

				helpers.AssertEventuallyProcessExitsWith(0, func() (garden.Process, error) {
					return testConnection("tcp", googleIPAddress, tcpPort)
				})

				helpers.AssertEventuallyProcessExitsWith(1, func() (garden.Process, error) {
					return testConnection("tcp", blockedGoogleIPAddress, tcpPort)
				})
			})

			It("can be allowed by whitelisting ports", func() {

				helpers.AssertEventuallyProcessExitsWith(1, func() (garden.Process, error) {
					return testConnection("tcp", googleIPAddress, tcpPort)
				})

				openPort(garden.ProtocolTCP, tcpPort, "")

				helpers.AssertEventuallyProcessExitsWith(0, func() (garden.Process, error) {
					return testConnection("tcp", googleIPAddress, tcpPort)
				})

			})

			It("can be allowed by whitelisting both ip and port", func() {
				var blockedTCPPort uint16 = 443

				helpers.AssertEventuallyProcessExitsWith(1, func() (garden.Process, error) {
					return testConnection("tcp", googleIPAddress, tcpPort)
				})

				openPort(garden.ProtocolTCP, tcpPort, googleIPAddress)

				helpers.AssertEventuallyProcessExitsWith(0, func() (garden.Process, error) {
					return testConnection("tcp", googleIPAddress, tcpPort)
				})

				helpers.AssertEventuallyProcessExitsWith(1, func() (garden.Process, error) {
					return testConnection("tcp", blockedGoogleIPAddress, tcpPort)
				})

				helpers.AssertEventuallyProcessExitsWith(1, func() (garden.Process, error) {
					return testConnection("tcp", googleIPAddress, blockedTCPPort)
				})
			})
		})

		Describe("outbound udp traffic", func() {
			It("can be allowed by whitelisting udp ports", func() {

				helpers.AssertEventuallyProcessExitsWith(1, func() (garden.Process, error) {
					return testConnection("udp", googleDNSServer, udpPort)
				})
				openPort(garden.ProtocolUDP, udpPort, "")

				helpers.AssertEventuallyProcessExitsWith(0, func() (garden.Process, error) {
					return testConnection("udp", googleDNSServer, udpPort)
				})
			})
		})
	})
})
