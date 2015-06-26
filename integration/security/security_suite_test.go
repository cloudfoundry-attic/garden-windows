package security

import (
	"testing"

	"github.com/cloudfoundry-incubator/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
	"github.com/onsi/gomega/gexec"
	"github.com/tedsuo/ifrit"

	"github.com/cloudfoundry-incubator/garden-windows/integration/helpers"
)

var gardenBin, containerizerBin string

var gardenProcess ifrit.Process
var client garden.Client

func TestSecurity(t *testing.T) {

	BeforeSuite(func() {
		containerizerBin = helpers.BuildContainerizer()
		gardenBin = helpers.BuildGarden()
	})

	BeforeEach(func() {
		gardenArgs := []string{}
		gardenProcess, client = helpers.StartGarden(gardenBin, containerizerBin, gardenArgs...)
	})

	AfterEach(func() {
		helpers.StopGarden(gardenProcess, client)
	})

	AfterSuite(func() {
		gexec.CleanupBuildArtifacts()
	})

	RegisterFailHandler(Fail)
	RunSpecs(t, "Security Suite")
}
