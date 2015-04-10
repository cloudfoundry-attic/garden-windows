package lifecycle_test

import (
	"fmt"
	"os"
	"os/exec"
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

	garden_runner "github.com/cloudfoundry-incubator/garden-windows/integration/runner"
)

var gardenBin string

var containerizerBin = os.Getenv("CONTAINERIZER_BIN")
var containerizerRunner ifrit.Runner
var gardenRunner *garden_runner.Runner
var gardenProcess ifrit.Process

var client garden.Client

func startGarden(argv ...string) garden.Client {
	gardenAddr := fmt.Sprintf("127.0.0.1:45607")

	tmpDir := os.TempDir()

	gardenRunner = garden_runner.New("tcp4", gardenAddr, tmpDir, gardenBin, "http://127.0.0.1:48080")
	containerizerRunner = ginkgomon.New(ginkgomon.Config{
		Name:              "containerizer",
		Command:           exec.Command(containerizerBin, "127.0.0.1", "48080"),
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

func ensureGardenRunning() {
	if err := client.Ping(); err != nil {
		client = startGarden()
	}
	Expect(client.Ping()).ShouldNot(HaveOccurred())
}

func TestLifecycle(t *testing.T) {

	SynchronizedBeforeSuite(func() []byte {
		Expect(containerizerBin).To(BeAnExistingFile(), "Expected $CONTAINERIZER_BIN to be a file")
		gardenPath, err := gexec.Build("github.com/cloudfoundry-incubator/garden-windows", "-a", "-race", "-tags", "daemon")
		Expect(err).ShouldNot(HaveOccurred())
		return []byte(gardenPath)
	}, func(gardenPath []byte) {
		gardenBin = string(gardenPath)
	})

	AfterEach(func() {
		ensureGardenRunning()
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
