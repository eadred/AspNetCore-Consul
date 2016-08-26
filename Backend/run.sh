#!/bin/bash

# Determine the route gateway - this will be the docker host's address
export CONSUL_HOST=$(route | grep 'default' | awk '{print $2}')

dotnet run --server.urls http://0.0.0.0:5100
