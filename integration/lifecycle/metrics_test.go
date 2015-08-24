package lifecycle

import (
	"github.com/cloudfoundry-incubator/garden"

	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
)

var _ = Describe("Container Metrics", func() {
	var gardenArgs []string

	BeforeEach(func() {
		gardenArgs = []string{}
		client = startGarden(gardenArgs...)
	})

	Describe("for a single container", func() {
		var container garden.Container

		BeforeEach(func() {
			var err error
			container, err = client.Create(garden.ContainerSpec{})
			Expect(err).ToNot(HaveOccurred())
			StreamIn(container)
		})

		AfterEach(func() {
			client.Destroy(container.Handle())
		})

		It("returns metrics", func() {
			metrics, err := container.Metrics()
			Expect(err).ToNot(HaveOccurred())

			Expect(metrics.MemoryStat.TotalUsageTowardLimit).To(BeNumerically(">", 0), "Expected Memory Usage to be > 0")
			Expect(metrics.CPUStat.Usage).To(BeNumerically(">", 0), "Expected CPU Usage to be > 0")
			Expect(metrics.DiskStat.TotalBytesUsed).To(BeNumerically(">", 0), "Expected TotalBytesUsed to be > 0")
			Expect(metrics.DiskStat.ExclusiveBytesUsed).To(BeNumerically(">", 0), "Expected ExclusiveBytesUsed to be > 0")
		})
	})

	Describe("for many containers", func() {
		var handles []string
		BeforeEach(func() {
			container1, err := client.Create(garden.ContainerSpec{})
			Expect(err).ToNot(HaveOccurred())
			StreamIn(container1)
			container2, err := client.Create(garden.ContainerSpec{})
			Expect(err).ToNot(HaveOccurred())
			StreamIn(container2)
			handles = []string{container1.Handle(), container2.Handle()}
		})

		AfterEach(func() {
			client.Destroy(handles[0])
			client.Destroy(handles[1])
		})

		Describe(".BulkMetrics", func() {
			It("returns container metrics for the specified handles", func() {
				containers, err := client.Containers(nil)
				Expect(err).ToNot(HaveOccurred())
				Expect(containers).To(HaveLen(2))

				bulkMetrics, err := client.BulkMetrics(handles)
				Expect(err).ToNot(HaveOccurred())
				Expect(bulkMetrics).To(HaveLen(2))
				for _, containerMetricsEntry := range bulkMetrics {
					Expect(containerMetricsEntry.Err).ToNot(HaveOccurred())
					Expect(containerMetricsEntry.Metrics.MemoryStat.TotalUsageTowardLimit).To(BeNumerically(">", 0))
					Expect(containerMetricsEntry.Metrics.CPUStat.Usage).To(BeNumerically(">", 0))
					Expect(containerMetricsEntry.Metrics.DiskStat.TotalBytesUsed).To(BeNumerically(">", 0))
					Expect(containerMetricsEntry.Metrics.DiskStat.ExclusiveBytesUsed).To(BeNumerically(">", 0))
				}
			})
		})
	})
})
