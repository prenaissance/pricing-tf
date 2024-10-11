#!/bin/bash

# find out hostname
hostname=$HOST || $HOSTNAME || $(hostname)

# skip registration if $ETCD_HOSTS is not set
if [ -z "$ETCD_HOSTS" ]; then
  echo "ETCD_HOSTS is not set, skipping service registration"
  exit 0
fi

SERVICE_NAME=pricing-tf

# register service
echo "Registering service $SERVICE_NAME with etcd hosts $ETCD_HOSTS"
export ETCDCTL_API=3
etcdctl --endpoints=$ETCD_HOSTS put /services/$SERVICE_NAME/$hostname `\
  { "host": "$hostname", "healthCheck":\
    { "port": 8080, "path": "/healthz/liveness", "delaySeconds": 5}\
  }` > /dev/null
echo "Service $SERVICE_NAME registered with etcd hosts $ETCD_HOSTS"