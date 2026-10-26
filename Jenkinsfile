pipeline {
    agent { label 'build-agent' }
    options {
        skipDefaultCheckout(true)
        disableConcurrentBuilds()
        timestamps()
    }
    environment {
        CONFIGURATION = 'Release'
        REGISTRY = 'localhost:5000'
        IMAGE_NAME = 'tournament-api'
    }
    stages {
        stage('Checkout') {
            steps {
                checkout scm
                sh 'git log -1 --oneline'
            }
        }
        stage('Build') {
            steps {
                sh '''
                    dotnet restore TournamentServices.slnx
                    dotnet build TournamentServices.slnx --configuration "$CONFIGURATION" --no-restore
                '''
            }
        }
        stage('Unit Test') {
            steps {
                sh '''
                    rm -rf TestResults
                    dotnet test TournamentServices.slnx \
                        --configuration "$CONFIGURATION" --no-build \
                        --logger "trx;LogFilePrefix=unit-tests" \
                        --results-directory TestResults
                '''
            }
            post { always { archiveArtifacts artifacts: 'TestResults/**/*', allowEmptyArchive: true } }
        }
        stage('Version') {
            steps {
                script {
                    env.SHORT_COMMIT = sh(script: 'git rev-parse --short=7 HEAD', returnStdout: true).trim()
                    env.IMAGE_TAG = "git-${env.SHORT_COMMIT}"
                    env.IMAGE = "${env.REGISTRY}/${env.IMAGE_NAME}:${env.IMAGE_TAG}"
                }
                echo "Build #${env.BUILD_NUMBER} -> release image: ${env.IMAGE}"
            }
        }
        stage('Package Image') {
            steps {
                sh '''
                    rm -rf out
                    mkdir -p out
                    dotnet publish src/TournamentServices.Api \
                        --configuration "$CONFIGURATION" --os linux --arch x64 \
                        -p:PublishProfile=DefaultContainer \
                        -p:ContainerRepository="$IMAGE_NAME" \
                        -p:ContainerImageTag="$IMAGE_TAG" \
                        -p:ContainerArchiveOutputPath="$WORKSPACE/out/$IMAGE_NAME.tar"
                    podman load --input "out/$IMAGE_NAME.tar"
                    podman tag "$IMAGE_NAME:$IMAGE_TAG" "$IMAGE"
                '''
            }
        }
        stage('Publish Image') {
            steps {
                sh '''
                    podman push --tls-verify=false "$IMAGE"
                    podman image inspect "$IMAGE" --format 'Publicada {{.Id}}'
                '''
            }
        }
        stage('Deploy Dev') {
            agent { label 'deploy-agent' }
            steps {
                checkout scm
                sh './scripts/deploy.sh kind-dev dev "$IMAGE"'
            }
        }
    }
    post {
        success { echo "Imagen publicada: ${env.IMAGE}" }
        unsuccessful { echo "Pipeline fallo en el commit ${env.GIT_COMMIT}" }
        always {
            archiveArtifacts artifacts: 'TestResults/**/*', allowEmptyArchive: true
            sh 'rm -rf out'
        }
    }
}
