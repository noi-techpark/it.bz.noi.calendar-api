pipeline {
    agent any

    environment {
	ASPNETCORE_ENVIRONMENT = "Development"
        DOCKER_PROJECT_NAME = "noi-calendar-api"
        DOCKER_IMAGE = '755952719952.dkr.ecr.eu-west-1.amazonaws.com/noi-calendar-api'
        DOCKER_TAG = "test-$BUILD_NUMBER"
	SERVER_PORT = "1044"
	CLIENT_ID = credentials('noi-calendar-api-test-client-id')
	CLIENT_SECRET = credentials('noi-calendar-api-test-client-secret')
	SERVICE_SCOPE = credentials('noi-calendar-api-test-scope')
	SERVICE_URL = credentials('noi-calendar-api-test-service-url')
	TENANT_ID = credentials('noi-calendar-api-test-tenant-id')
	OPENID_AUTHORITY = "https://auth.opendatahub.testingmachine.eu/auth/realms/noi/"
    }

    stages {
        stage('Configure') {
            steps {
                sh """
                    rm -f .env
                    cp .env.example .env
                    echo 'COMPOSE_PROJECT_NAME=${DOCKER_PROJECT_NAME}' > .env
                    echo 'DOCKER_IMAGE=${DOCKER_IMAGE}' >> .env
                    echo 'DOCKER_TAG=${DOCKER_TAG}' >> .env
                    echo 'SERVER_PORT=${SERVER_PORT}' >> .env         
		    echo 'ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}' >> .env         
		    echo 'CLIENT_ID=${CLIENT_ID}' >> .env   
		    echo 'CLIENT_SECRET=${CLIENT_SECRET}' >> .env   
		    echo 'SERVICE_SCOPE=${SERVICE_SCOPE}' >> .env   
		    echo 'SERVICE_URL=${SERVICE_URL}' >> .env   
		    echo 'TENANT_ID=${TENANT_ID}' >> .env   
		    echo 'OPENID_AUTHORITY=${OPENID_AUTHORITY}' >> .env   		    
                """
            }
        }
        stage('Build') {
            steps {
                sh '''
                    aws ecr get-login --region eu-west-1 --no-include-email | bash
                    docker-compose --no-ansi -f docker-compose.yml build --pull
                    docker-compose --no-ansi -f docker-compose.yml push
                '''
            }
        }
        stage('Deploy') {
            steps {
               sshagent(['jenkins-ssh-key']) {
                    sh """
                        (cd infrastructure/ansible && ansible-galaxy install -f -r requirements.yml)
                        (cd infrastructure/ansible && ansible-playbook --limit=test deploy.yml --extra-vars "release_name=${BUILD_NUMBER}")
                    """
                }
            }
        }	
    }
}
