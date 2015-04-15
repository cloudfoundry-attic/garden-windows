package containerizer_url_test

import (
	"github.com/cloudfoundry-incubator/garden"
	"github.com/cloudfoundry-incubator/garden-windows/containerizer_url"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
)

var _ = Describe("ContainerizerURL", func() {
	Context("when the base URL is able to be parsed", func() {
		AssertUrls := func(base string) {
			containerizerURL, _ := containerizer_url.FromString(base)
			expectedPing := "http://127.0.0.1:1788/api/ping"
			expectedCreate := "http://127.0.0.1:1788/api/containers"
			expectedDestroy := "http://127.0.0.1:1788/api/containers/handle"
			expectedList := "http://127.0.0.1:1788/api/containers"
			expectedListWithProps := "http://127.0.0.1:1788/api/containers?q=%7B%22a%22%3A%22c%22%7D"
			expectedBulkInfo := "http://127.0.0.1:1788/api/bulkcontainerinfo"

			Expect(containerizerURL.Ping()).Should(Equal(expectedPing))
			Expect(containerizerURL.Create()).Should(Equal(expectedCreate))
			Expect(containerizerURL.Destroy("handle")).Should(Equal(expectedDestroy))
			Expect(containerizerURL.List(nil)).Should(Equal(expectedList))
			Expect(containerizerURL.List(garden.Properties{"a": "c"})).Should(Equal(expectedListWithProps))
			Expect(containerizerURL.BulkInfo()).Should(Equal(expectedBulkInfo))

			expectedStop := "http://127.0.0.1:1788/api/containers/handle/stop"
			expectedGetProperties := "http://127.0.0.1:1788/api/containers/handle/properties"
			expectedInfo := "http://127.0.0.1:1788/api/containers/handle/info"
			expectedStreamIn := "http://127.0.0.1:1788/api/containers/handle/files?destination=%2F"
			expectedStreamOut := "http://127.0.0.1:1788/api/containers/handle/files?source=%2F"
			expectedNetIn := "http://127.0.0.1:1788/api/containers/handle/net/in"

			expectedRun := "ws://127.0.0.1:1788/api/containers/handle/run"
			expectedGetProperty := "http://127.0.0.1:1788/api/containers/handle/properties/prop"
			expectedSetProperty := "http://127.0.0.1:1788/api/containers/handle/properties/prop"
			expectedRemoveProperty := "http://127.0.0.1:1788/api/containers/handle/properties/prop"

			Expect(containerizerURL.Stop("handle")).Should(Equal(expectedStop))
			Expect(containerizerURL.GetProperties("handle")).Should(Equal(expectedGetProperties))
			Expect(containerizerURL.Info("handle")).Should(Equal(expectedInfo))
			Expect(containerizerURL.StreamIn("handle", "/")).Should(Equal(expectedStreamIn))
			Expect(containerizerURL.StreamOut("handle", "/")).Should(Equal(expectedStreamOut))
			Expect(containerizerURL.NetIn("handle")).Should(Equal(expectedNetIn))
			Expect(containerizerURL.Run("handle")).Should(Equal(expectedRun))
			Expect(containerizerURL.GetProperty("handle", "prop")).Should(Equal(expectedGetProperty))
			Expect(containerizerURL.SetProperty("handle", "prop")).Should(Equal(expectedSetProperty))
			Expect(containerizerURL.RemoveProperty("handle", "prop")).Should(Equal(expectedRemoveProperty))
		}

		It("works when the base URL does not have a trailing slash", func() {
			AssertUrls("http://127.0.0.1:1788")
		})

		It("works when the base URL does have a trailing slash", func() {
			AssertUrls("http://127.0.0.1:1788/")
		})
	})

	Context("when the base URL is not absolute (i.e. relative)", func() {
		It("returns an error", func() {
			_, err := containerizer_url.FromString("127.0.0.1:8080")
			Expect(err).To(HaveOccurred())
		})

		It("returns an error with leading slashes", func() {
			_, err := containerizer_url.FromString("//127.0.0.1:8080")
			Expect(err).To(HaveOccurred())
		})
	})

	Context("when the base URL is non readable", func() {
		It("returns an error", func() {
			_, err := containerizer_url.FromString("sxf$5%")
			Expect(err).To(HaveOccurred())
		})
	})
})
