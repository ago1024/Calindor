cd..
FOR /F "tokens=1* delims= " %%G IN ('cd') DO SET "top_dir=%%G"
cd release
FOR /F "tokens=1 delims= " %%G IN ('Calindor.exe /pv') DO SET "calindor_version=%%G"
SET "dest_dir=Calindor_%calindor_version%_src"
cd..
cd setup
rmdir %dest_dir% /S /Q
mkdir %dest_dir%
cd..
xcopy src\* setup\%dest_dir%\ /E
cd setup
cd %dest_dir%
cd server
rmdir obj /S /Q
cd..
cd..
cd..
copy doc\Installation.txt setup\%dest_dir%
copy doc\Compilation.txt setup\%dest_dir%
copy doc\License.txt setup\%dest_dir%
cd "%top_dir%\setup"
7za a -r -tzip %dest_dir%.zip %dest_dir%\*