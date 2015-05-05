package main

import (
	"fmt"
	"net/http"
	"os"
	"time"
)

func main() {
	client := http.Client{
		Timeout: time.Duration(1 * time.Second),
	}
	response, err := client.Get(os.Getenv("URL"))
	if err != nil || response.StatusCode != 200 {
		os.Exit(1)
	}
	fmt.Printf("Connected successfully to %s", os.Getenv("URL"))
}
