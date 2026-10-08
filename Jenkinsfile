// Actividad 3: pruebas automaticas como quality gate
pipeline {
    agent {
        label 'build-agent'
    }

    options {
        skipDefaultCheckout(true)
        disableConcurrentBuilds()
    }

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
                // LogFilePrefix (no LogFileName): hay 4 proyectos de test y con
                // un nombre fijo cada uno sobreescribiria el .trx del anterior.
                // Las pruebas de Postgres se saltan solas (no hay TEST_POSTGRES).
                sh '''
                    rm -rf TestResults
                    dotnet test TournamentServices.slnx \
                        --configuration "$CONFIGURATION" \
                        --no-build \
                        --logger "trx;LogFilePrefix=unit-tests" \
                        --results-directory TestResults
                '''
            }
            post {
                always {
                    archiveArtifacts artifacts: 'TestResults/**/*',
                                     allowEmptyArchive: true
                }
            }
        }
    }
}
