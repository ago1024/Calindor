FROM mono AS build
WORKDIR /app

# Copy sources
COPY src src
COPY el-tools el-tools

# Build server and csu
RUN msbuild src/server/server.csproj /p:Configuration=Release
RUN msbuild src/csu/csu.csproj /p:Configuration=Release

FROM build AS publish
WORKDIR /app

# Copy built files to output dir
RUN cp -r release ./out
# Cleanup debug files
RUN rm -f ./out/*.pdb

FROM mono:slim AS runtime
MAINTAINER Alexander Gottwald <alexander.gottwald@gmail.com>
LABEL Description="Calindor server"
WORKDIR /app

# Copy published file
COPY --from=publish /app/out/* ./
COPY docker/server_config.xml .
COPY data/maps/calindor_startmap.elm ./data/maps/

STOPSIGNAL SIGINT
EXPOSE 4242

VOLUME /srv/calindor
CMD ["mono", "calindor.exe"]

