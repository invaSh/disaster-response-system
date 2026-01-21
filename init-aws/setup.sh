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

# creating SNS topic for incident updated events
awslocal sns create-topic --name incident-updated-topic
UPDATED_TOPIC_ARN=$(awslocal sns list-topics --query 'Topics[?contains(TopicArn, `incident-updated-topic`)].TopicArn' --output text)
echo "Created SNS topic: $UPDATED_TOPIC_ARN"

# creating SQS queues for updated event consumers
awslocal sqs create-queue --queue-name dispatch-incident-updated-queue
awslocal sqs create-queue --queue-name notification-incident-updated-queue

# Get queue URLs
DISPATCH_UPDATED_QUEUE_URL=$(awslocal sqs get-queue-url --queue-name dispatch-incident-updated-queue --query 'QueueUrl' --output text)
NOTIFICATION_UPDATED_QUEUE_URL=$(awslocal sqs get-queue-url --queue-name notification-incident-updated-queue --query 'QueueUrl' --output text)

# Get queue ARNs
DISPATCH_UPDATED_QUEUE_ARN=$(awslocal sqs get-queue-attributes --queue-url "$DISPATCH_UPDATED_QUEUE_URL" --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)
NOTIFICATION_UPDATED_QUEUE_ARN=$(awslocal sqs get-queue-attributes --queue-url "$NOTIFICATION_UPDATED_QUEUE_URL" --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)

# Subscribe queues to SNS topic
awslocal sns subscribe \
  --topic-arn "$UPDATED_TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$DISPATCH_UPDATED_QUEUE_ARN"

awslocal sns subscribe \
  --topic-arn "$UPDATED_TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$NOTIFICATION_UPDATED_QUEUE_ARN"

# Set queue policies
awslocal sqs set-queue-attributes \
  --queue-url "$DISPATCH_UPDATED_QUEUE_URL" \
  --attributes Policy="{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Principal\":\"*\",\"Action\":\"sqs:SendMessage\",\"Resource\":\"$DISPATCH_UPDATED_QUEUE_ARN\",\"Condition\":{\"ArnEquals\":{\"aws:SourceArn\":\"$UPDATED_TOPIC_ARN\"}}}]}"

awslocal sqs set-queue-attributes \
  --queue-url "$NOTIFICATION_UPDATED_QUEUE_URL" \
  --attributes Policy="{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Principal\":\"*\",\"Action\":\"sqs:SendMessage\",\"Resource\":\"$NOTIFICATION_UPDATED_QUEUE_ARN\",\"Condition\":{\"ArnEquals\":{\"aws:SourceArn\":\"$UPDATED_TOPIC_ARN\"}}}]}"

# creating SNS topic for incident deleted events
awslocal sns create-topic --name incident-deleted-topic
DELETED_TOPIC_ARN=$(awslocal sns list-topics --query 'Topics[?contains(TopicArn, `incident-deleted-topic`)].TopicArn' --output text)
echo "Created SNS topic: $DELETED_TOPIC_ARN"

# creating SQS queues for deleted event consumers
awslocal sqs create-queue --queue-name dispatch-incident-deleted-queue
awslocal sqs create-queue --queue-name notification-incident-deleted-queue

# Get queue URLs
DISPATCH_DELETED_QUEUE_URL=$(awslocal sqs get-queue-url --queue-name dispatch-incident-deleted-queue --query 'QueueUrl' --output text)
NOTIFICATION_DELETED_QUEUE_URL=$(awslocal sqs get-queue-url --queue-name notification-incident-deleted-queue --query 'QueueUrl' --output text)

# Get queue ARNs
DISPATCH_DELETED_QUEUE_ARN=$(awslocal sqs get-queue-attributes --queue-url "$DISPATCH_DELETED_QUEUE_URL" --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)
NOTIFICATION_DELETED_QUEUE_ARN=$(awslocal sqs get-queue-attributes --queue-url "$NOTIFICATION_DELETED_QUEUE_URL" --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)

# Subscribe queues to SNS topic
awslocal sns subscribe \
  --topic-arn "$DELETED_TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$DISPATCH_DELETED_QUEUE_ARN"

awslocal sns subscribe \
  --topic-arn "$DELETED_TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$NOTIFICATION_DELETED_QUEUE_ARN"

# Set queue policies
awslocal sqs set-queue-attributes \
  --queue-url "$DISPATCH_DELETED_QUEUE_URL" \
  --attributes Policy="{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Principal\":\"*\",\"Action\":\"sqs:SendMessage\",\"Resource\":\"$DISPATCH_DELETED_QUEUE_ARN\",\"Condition\":{\"ArnEquals\":{\"aws:SourceArn\":\"$DELETED_TOPIC_ARN\"}}}]}"

awslocal sqs set-queue-attributes \
  --queue-url "$NOTIFICATION_DELETED_QUEUE_URL" \
  --attributes Policy="{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Principal\":\"*\",\"Action\":\"sqs:SendMessage\",\"Resource\":\"$NOTIFICATION_DELETED_QUEUE_ARN\",\"Condition\":{\"ArnEquals\":{\"aws:SourceArn\":\"$DELETED_TOPIC_ARN\"}}}]}"

echo "Created all SQS queues and subscribed to SNS topics"
echo "Dispatch Updated Queue URL: $DISPATCH_UPDATED_QUEUE_URL"
echo "Notification Updated Queue URL: $NOTIFICATION_UPDATED_QUEUE_URL"
echo "Dispatch Deleted Queue URL: $DISPATCH_DELETED_QUEUE_URL"
echo "Notification Deleted Queue URL: $NOTIFICATION_DELETED_QUEUE_URL"

# creating SNS topic for dispatch events
awslocal sns create-topic --name dispatch-events-topic
DISPATCH_EVENTS_TOPIC_ARN=$(awslocal sns list-topics --query 'Topics[?contains(TopicArn, `dispatch-events-topic`)].TopicArn' --output text)
echo "Created SNS topic: $DISPATCH_EVENTS_TOPIC_ARN"

# creating SQS queue for dispatch events consumer (IncidentService)
awslocal sqs create-queue --queue-name incident-dispatch-queue

# Get queue URL
INCIDENT_DISPATCH_QUEUE_URL=$(awslocal sqs get-queue-url --queue-name incident-dispatch-queue --query 'QueueUrl' --output text)

# Get queue ARN
INCIDENT_DISPATCH_QUEUE_ARN=$(awslocal sqs get-queue-attributes --queue-url "$INCIDENT_DISPATCH_QUEUE_URL" --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)

# Subscribe queue to SNS topic
awslocal sns subscribe \
  --topic-arn "$DISPATCH_EVENTS_TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$INCIDENT_DISPATCH_QUEUE_ARN"

# Set queue policy to allow SNS to send messages
awslocal sqs set-queue-attributes \
  --queue-url "$INCIDENT_DISPATCH_QUEUE_URL" \
  --attributes Policy="{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Principal\":\"*\",\"Action\":\"sqs:SendMessage\",\"Resource\":\"$INCIDENT_DISPATCH_QUEUE_ARN\",\"Condition\":{\"ArnEquals\":{\"aws:SourceArn\":\"$DISPATCH_EVENTS_TOPIC_ARN\"}}}]}"

echo "Created dispatch events SNS topic and incident-dispatch-queue"
echo "Incident Dispatch Queue URL: $INCIDENT_DISPATCH_QUEUE_URL"

echo "########### Initialization Complete ###########"