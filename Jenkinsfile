// Actividad 1: primer pipeline
// Solo verifica que cada agente tenga sus herramientas.
pipeline {
    agent {
        label 'build-agent'
    }

    stages {
        stage('Build Environment') {
            steps {
                sh '''
                    hostname
                    dotnet --info
                    podman version
                '''
            }
        }

        stage('Deployment Environment') {
            agent {
                label 'deploy-agent'
            }
            steps {
                sh '''
                    hostname
                    git --version
                    kubectl version --client
                    curl --version
                '''
            }
        }
    }
}
