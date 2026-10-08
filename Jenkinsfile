// Actividad 4: identidad inmutable del artefacto (tag = commit)
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

        stage('Version') {
            steps {
                script {
                    // mismo commit = mismo tag, sin importar el numero de build
                    env.SHORT_COMMIT = sh(
                        script: 'git rev-parse --short=7 HEAD',
                        returnStdout: true
                    ).trim()

                    env.IMAGE_TAG = "git-${env.SHORT_COMMIT}"
                    env.IMAGE = "${env.REGISTRY}/${env.IMAGE_NAME}:${env.IMAGE_TAG}"
                }

                echo "Build #${env.BUILD_NUMBER} -> release image: ${env.IMAGE}"
            }
        }

        stage('Package Image') {
            steps {
                // Sin --no-build: la imagen necesita un build para linux-x64 y el
                // stage Build compilo sin RID (otra carpeta en bin/), asi que
                // --no-build no encontraria los binarios. Es el mismo commit
                // que ya paso las pruebas.
                // ContainerArchiveOutputPath: el SDK escribe un .tar en vez de
                // buscar un docker daemon, y podman lo carga por el socket.
                sh '''
                    rm -rf out
                    mkdir -p out

                    dotnet publish src/TournamentServices.Api \
                        --configuration "$CONFIGURATION" \
                        --os linux \
                        --arch x64 \
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
                // registry local sin TLS. Ojo: podman corre en el host (socket),
                // por eso localhost:5000 si llega al registry; un curl desde
                // este contenedor a localhost NO llegaria
                sh '''
                    podman push --tls-verify=false "$IMAGE"
                    podman image inspect "$IMAGE" --format 'Publicada {{.Id}}'
                '''
            }
        }
    }
}
