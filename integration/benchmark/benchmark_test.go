package benchmark_test

import (
	"github.com/cloudfoundry-incubator/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
	"os"
)

var _ = Describe("Benchmark", func() {
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

	Describe("StreamIn", func() {
		Measure("it should stream in fast", func(b Benchmarker) {
			runtime := b.Time("runtime", func() {
				tarFile, err := os.Open("../bin/consume.tgz")
				Expect(err).ShouldNot(HaveOccurred())
				defer tarFile.Close()

				err = c.StreamIn(garden.StreamInSpec{Path: "bin", TarStream: tarFile})
				Expect(err).ShouldNot(HaveOccurred())
			})

			Expect(runtime.Seconds()).Should(BeNumerically("<", 100), "StreamIn() shouldn't take too long.")
		}, 10)

		XMeasure("it should stream in big files fast", func(b Benchmarker) {
			runtime := b.Time("runtime", func() {
				tarFile, err := os.Open("../bin/garden-linux-release-0.325.0.tgz")
				Expect(err).ShouldNot(HaveOccurred())
				defer tarFile.Close()

				err = c.StreamIn(garden.StreamInSpec{Path: "bin", TarStream: tarFile})
				Expect(err).ShouldNot(HaveOccurred())
			})

			Expect(runtime.Seconds()).Should(BeNumerically("<", 300), "StreamIn() shouldn't take too long.")
		}, 10)

	})
})
