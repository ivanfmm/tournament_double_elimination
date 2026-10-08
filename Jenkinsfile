// Actividad 2: checkout y build
pipeline {
    agent {
        label 'build-agent'
    }

    options {
        // el job ya sabe de que repo/rama/commit viene (Multibranch);
        // hacemos el checkout explicito en el stage Checkout
        skipDefaultCheckout(true)
        // dos builds a la vez pisarian el mismo cluster/registry
        disableConcurrentBuilds()
    }

    environment {
        CONFIGURATION = 'Release'
    }

    stages {
        stage('Checkout') {
            steps {
                // checkout scm usa la URL, rama y commit configurados en el job
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
    }
}
