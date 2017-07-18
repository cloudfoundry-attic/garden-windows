package benchmark_test

import (
	"testing"

	"code.cloudfoundry.org/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
	"github.com/onsi/gomega/gexec"
	"github.com/tedsuo/ifrit"

	"code.cloudfoundry.org/garden-windows/integration/helpers"
	garden_runner "code.cloudfoundry.org/garden-windows/integration/runner"
)

var gardenBin, containerizerBin string

var containerizerRunner ifrit.Runner
var gardenRunner *garden_runner.Runner
var gardenProcess ifrit.Process
var containerizerPort int
var client garden.Client

func startGarden(argv ...string) garden.Client {
	gardenProcess, client = helpers.StartGarden(gardenBin, containerizerBin, 0, argv...)
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
	RunSpecs(t, "Benchmark Suite")
}
