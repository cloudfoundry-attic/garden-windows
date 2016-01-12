package main

import (
	"fmt"
	"os"
)

func main() {
	path := os.Args[1]
	f, err := os.Open(path)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error opening path %s: %s", path, err)
		os.Exit(1)
	}
	defer f.Close()
	names, err := f.Readdirnames(-1)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error reading path %s: %s", path, err)
		os.Exit(1)
	}
	for _, n := range names {
		fmt.Println(n)
	}
	os.Exit(0)
}
