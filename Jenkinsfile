pipeline {
    agent { label 'build-agent' }
    options { skipDefaultCheckout(true); disableConcurrentBuilds() }
    environment {
        CONFIGURATION = 'Release'
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
    }
}
