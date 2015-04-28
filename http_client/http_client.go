package http_client

import (
	"bytes"
	"encoding/json"
	"errors"
	"io"
	"io/ioutil"
	"net/http"
	"strings"

	"github.com/pivotal-golang/lager"
)

type Client struct {
	logger lager.Logger
}

func NewClient(logger lager.Logger) *Client {
	return &Client{logger: logger}
}

func (client *Client) Get(url string, output interface{}) error {
	response, err := http.Get(url)
	err = client.parseJson(url, response, err, output)
	return err
}

func (client *Client) Post(url string, payload, output interface{}) error {
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
	req, err := http.NewRequest("DELETE", url, strings.NewReader(""))
	if err != nil {
		return err
	}
	response, err := http.DefaultClient.Do(req)
	err = client.parseJson(url, response, err, nil)
	return err
}

func (client *Client) parseJson(url string, response *http.Response, err error, output interface{}) error {
	if err != nil {
		client.logger.Info("ERROR GETTING JSON", lager.Data{"error": err, "url": url})
		return err
	}
	defer response.Body.Close()
	rawJSON, err := ioutil.ReadAll(response.Body)
	if err != nil {
		return err
	}

	if response.StatusCode < 200 || response.StatusCode >= 300 {
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
