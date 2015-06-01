package main

import (
	"os"
	"os/exec"
)

func main() {
	prog := os.Args[1]

	cmd := exec.Command(prog)
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	cmd.Run()
}
