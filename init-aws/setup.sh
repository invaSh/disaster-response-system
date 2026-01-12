#!/bin/bash
echo "########### Initializing LocalStack Resources ###########"

# creating incident attachments bucket
awslocal s3 mb s3://incident-attachments

# creating SNS topic for incident events
awslocal sns create-topic --name incident-created-topic
INCIDENT_TOPIC_ARN=$(awslocal sns list-topics --query 'Topics[?contains(TopicArn, `incident-created-topic`)].TopicArn' --output text)
echo "Created SNS topic: $INCIDENT_TOPIC_ARN"

# creating SQS queues for consumers
awslocal sqs create-queue --queue-name dispatch-incident-queue
awslocal sqs create-queue --queue-name notification-incident-queue

# Get queue URLs
DISPATCH_QUEUE_URL=$(awslocal sqs get-queue-url --queue-name dispatch-incident-queue --query 'QueueUrl' --output text)
NOTIFICATION_QUEUE_URL=$(awslocal sqs get-queue-url --queue-name notification-incident-queue --query 'QueueUrl' --output text)

# Get queue ARNs (needed for subscription)
DISPATCH_QUEUE_ARN=$(awslocal sqs get-queue-attributes --queue-url "$DISPATCH_QUEUE_URL" --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)
NOTIFICATION_QUEUE_ARN=$(awslocal sqs get-queue-attributes --queue-url "$NOTIFICATION_QUEUE_URL" --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)

# Subscribe queues to SNS topic
awslocal sns subscribe \
  --topic-arn "$INCIDENT_TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$DISPATCH_QUEUE_ARN"

awslocal sns subscribe \
  --topic-arn "$INCIDENT_TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$NOTIFICATION_QUEUE_ARN"

# Set queue policy to allow SNS to send messages
awslocal sqs set-queue-attributes \
  --queue-url "$DISPATCH_QUEUE_URL" \
  --attributes Policy="{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Principal\":\"*\",\"Action\":\"sqs:SendMessage\",\"Resource\":\"$DISPATCH_QUEUE_ARN\",\"Condition\":{\"ArnEquals\":{\"aws:SourceArn\":\"$INCIDENT_TOPIC_ARN\"}}}]}"

awslocal sqs set-queue-attributes \
  --queue-url "$NOTIFICATION_QUEUE_URL" \
  --attributes Policy="{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Principal\":\"*\",\"Action\":\"sqs:SendMessage\",\"Resource\":\"$NOTIFICATION_QUEUE_ARN\",\"Condition\":{\"ArnEquals\":{\"aws:SourceArn\":\"$INCIDENT_TOPIC_ARN\"}}}]}"

echo "Created SQS queues and subscribed to SNS topic"
echo "Dispatch Queue URL: $DISPATCH_QUEUE_URL"
echo "Notification Queue URL: $NOTIFICATION_QUEUE_URL"

echo "########### Initialization Complete ###########"