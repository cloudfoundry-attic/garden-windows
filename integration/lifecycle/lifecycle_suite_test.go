package lifecycle

import (
	"testing"

	"code.cloudfoundry.org/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
	"github.com/onsi/gomega/gexec"
	"github.com/tedsuo/ifrit"

	"github.com/cloudfoundry/garden-windows/integration/helpers"
	garden_runner "github.com/cloudfoundry/garden-windows/integration/runner"
)

var gardenBin, containerizerBin string

var containerizerRunner ifrit.Runner
var gardenRunner *garden_runner.Runner
var gardenProcess ifrit.Process
var containerizerPort int
var client garden.Client

func startGarden(argv ...string) garden.Client {
	gardenProcess, client = helpers.StartGarden(gardenBin, containerizerBin, argv...)
	return client
}

func TestLifecycle(t *testing.T) {

	BeforeSuite(func() {
		containerizerBin = helpers.BuildContainerizer()
		gardenBin = helpers.BuildGarden()
	})

	BeforeEach(func() {
		helpers.KillAllGarden()
		helpers.KillAllContainerizer()
	})

	AfterEach(func() {
		helpers.StopGarden(gardenProcess, client)
	})

	AfterSuite(func() {
		gexec.CleanupBuildArtifacts()
	})

	RegisterFailHandler(Fail)
	RunSpecs(t, "Lifecycle Suite")
}
