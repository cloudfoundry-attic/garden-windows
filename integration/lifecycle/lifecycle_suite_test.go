package lifecycle

import (
	"fmt"
	"os"
	"os/exec"
	"strconv"
	"syscall"
	"testing"
	"time"

	"github.com/cloudfoundry-incubator/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
	"github.com/onsi/gomega/gexec"
	"github.com/tedsuo/ifrit"
	"github.com/tedsuo/ifrit/ginkgomon"
	"github.com/tedsuo/ifrit/grouper"

	"github.com/cloudfoundry-incubator/garden-windows/integration/helpers"
	garden_runner "github.com/cloudfoundry-incubator/garden-windows/integration/runner"
)

var gardenBin, containerizerBin string

var containerizerRunner ifrit.Runner
var gardenRunner *garden_runner.Runner
var gardenProcess ifrit.Process
var containerizerPort int
var client garden.Client

func startGarden(argv ...string) garden.Client {
	gardenAddr := fmt.Sprintf("127.0.0.1:45607")

	tmpDir := os.TempDir()

	// If below fails, try
	// netsh advfirewall firewall add rule name="Open Port 48080"  dir=in action=allow protocol=TCP localport=48080

	containerizerPort = 48081
	gardenRunner = garden_runner.New("tcp4", gardenAddr, tmpDir, gardenBin, fmt.Sprintf("http://127.0.0.1:%d", containerizerPort))
	containerizerRunner = ginkgomon.New(ginkgomon.Config{
		Name:              "containerizer",
		Command:           exec.Command(containerizerBin, "127.0.0.1", strconv.Itoa(containerizerPort)),
		AnsiColorCode:     "",
		StartCheck:        "Control-C to quit.",
		StartCheckTimeout: 10 * time.Second,
		Cleanup:           func() {},
	})

	group := grouper.NewOrdered(syscall.SIGTERM, []grouper.Member{
		{Name: "containerizer", Runner: containerizerRunner},
		{Name: "garden", Runner: gardenRunner},
	})

	gardenProcess = ifrit.Invoke(group)

	return gardenRunner.NewClient()
}

func restartGarden(argv ...string) {
	Expect(client.Ping()).Should(Succeed(), "tried to restart garden while it was not running")
	gardenProcess.Signal(syscall.SIGTERM)
	Eventually(gardenProcess.Wait(), 10).Should(Receive())

	startGarden(argv...)
}

func TestLifecycle(t *testing.T) {

	SynchronizedBeforeSuite(func() []byte {
		containerizerBin = helpers.BuildContainerizer()
		gardenPath := helpers.BuildGarden()
		return []byte(gardenPath)
	}, func(gardenPath []byte) {
		gardenBin = string(gardenPath)
	})

	AfterEach(func() {
		gardenProcess.Signal(syscall.SIGKILL)
		Eventually(gardenProcess.Wait(), 10).Should(Receive())
	})

	SynchronizedAfterSuite(func() {
		//noop
	}, func() {
		gexec.CleanupBuildArtifacts()
	})

	RegisterFailHandler(Fail)
	RunSpecs(t, "Lifecycle Suite")
}

func containerIP(ctr garden.Container) string {
	info, err := ctr.Info()
	Expect(err).ShouldNot(HaveOccurred())
	return info.ContainerIP
}

func dumpIP() {
	cmd := exec.Command("ip", "a")
	op, err := cmd.CombinedOutput()
	Expect(err).ShouldNot(HaveOccurred())
	fmt.Println("IP status:\n", string(op))

	cmd = exec.Command("iptables", "--list")
	op, err = cmd.CombinedOutput()
	Expect(err).ShouldNot(HaveOccurred())
	fmt.Println("IP tables chains:\n", string(op))

	cmd = exec.Command("iptables", "--list-rules")
	op, err = cmd.CombinedOutput()
	Expect(err).ShouldNot(HaveOccurred())
	fmt.Println("IP tables rules:\n", string(op))
}
