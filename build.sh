unzip fly.zip
docker buildx build --platform linux/amd64 --target final -f ConcourseWatcher/Dockerfile -t zlzforever/concourse-watcher .
rm -rf fly