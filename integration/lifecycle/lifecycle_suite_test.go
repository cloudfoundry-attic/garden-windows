package lifecycle

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
var lsExePath string

func startGarden(maxContainerProcs int, argv ...string) garden.Client {
	gardenProcess, client = helpers.StartGarden(gardenBin, containerizerBin, maxContainerProcs, argv...)
	return client
}

func TestLifecycle(t *testing.T) {

	BeforeSuite(func() {
		var err error

		containerizerBin = helpers.BuildContainerizer()
		gardenBin = helpers.BuildGarden()

		lsExePath, err = gexec.Build("code.cloudfoundry.org/garden-windows/integration/bin/ls-files", "-race")
		Expect(err).ShouldNot(HaveOccurred())
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
