package http_client

import (
	"bytes"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"io/ioutil"
	"net/http"
	neturl "net/url"
	"strings"

	"github.com/pivotal-golang/lager"
)

type Client struct {
	logger  lager.Logger
	baseUrl *neturl.URL
}

func NewClient(logger lager.Logger, baseUrl *neturl.URL) *Client {
	return &Client{logger: logger, baseUrl: baseUrl}
}

func (client *Client) ReadBody(url string) (io.ReadCloser, error) {
	url, err := client.addHostToUrl(url)
	if err != nil {
		return nil, err
	}
	response, err := http.Get(url)
	if !isSuccessful(response) {
		return nil, parseErrorFromResponse(response)
	}

	return response.Body, nil
}

func (client *Client) Get(url string, output interface{}) error {
	url, err := client.addHostToUrl(url)
	if err != nil {
		return err
	}
	response, err := http.Get(url)
	return client.parseJson(url, response, err, output)
}

func (client *Client) RunURL(handle string) string {
	url := *client.baseUrl
	url.Scheme = "ws"
	url.Path = fmt.Sprintf("/api/containers/%s/run", handle)
	return url.String()
}

func (client *Client) Post(url string, payload, output interface{}) error {
	url, err := client.addHostToUrl(url)
	if err != nil {
		return err
	}
	jsonPayload := []byte("{}")
	if payload != nil {
		var err error
		jsonPayload, err = json.Marshal(payload)
		if err != nil {
			return err
		}
	}
	response, err := http.Post(url, "application/json", bytes.NewBuffer(jsonPayload))
	err = client.parseJson(url, response, err, output)
	return err
}

func (client *Client) Put(url string, payload io.Reader, contentType string) error {
	url, err := client.addHostToUrl(url)
	if err != nil {
		return err
	}
	req, err := http.NewRequest("PUT", url, payload)
	if err != nil {
		return err
	}
	req.Header.Set("Content-Type", contentType)
	response, err := http.DefaultClient.Do(req)
	err = client.parseJson(url, response, err, nil)
	return err
}

func (client *Client) Delete(url string) error {
	url, err := client.addHostToUrl(url)
	if err != nil {
		return err
	}
	req, err := http.NewRequest("DELETE", url, strings.NewReader(""))
	if err != nil {
		return err
	}
	response, err := http.DefaultClient.Do(req)
	err = client.parseJson(url, response, err, nil)
	return err
}

func (client *Client) addHostToUrl(url string) (string, error) {
	uri, err := neturl.Parse(url)
	if err != nil {
		return "", err
	}
	uri.Scheme = client.baseUrl.Scheme
	uri.Host = client.baseUrl.Host
	return uri.String(), nil
}

func (client *Client) parseJson(url string, response *http.Response, err error, output interface{}) error {
	if err != nil {
		client.logger.Info("ERROR GETTING JSON", lager.Data{"error": err, "url": url})
		return err
	}

	if !isSuccessful(response) {
		return parseErrorFromResponse(response)
	}

	defer response.Body.Close()
	rawJSON, err := ioutil.ReadAll(response.Body)
	if err != nil {
		return err
	}

	if output != nil {
		err = json.Unmarshal(rawJSON, output)
		if err != nil {
			client.logger.Info("ERROR UNMARSHALING JSON", lager.Data{
				"url":   response.Request.URL.String(),
				"error": err,
			})
			return err
		}
	}
	return nil
}

func isSuccessful(response *http.Response) bool {
	return response.StatusCode >= 200 && response.StatusCode < 300
}

func parseErrorFromResponse(response *http.Response) error {
	defer response.Body.Close()
	rawJSON, err := ioutil.ReadAll(response.Body)
	if err != nil {
		return err
	}

	type exceptionStruct struct {
		ExceptionMessage string
	}
	exceptionData := exceptionStruct{}

	errorJson := json.Unmarshal(rawJSON, &exceptionData)
	if errorJson == nil && exceptionData.ExceptionMessage != "" {
		return errors.New(exceptionData.ExceptionMessage)
	}

	exceptionString := ""
	errorJson = json.Unmarshal(rawJSON, &exceptionString)
	if errorJson == nil && exceptionString != "" {
		return errors.New(exceptionString)
	}

	return errors.New(response.Status)
}
