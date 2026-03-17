1 -create remot repo
2 - run devcontainer-init command inside repo (which is cloned in local machine)
** .cmd extension of devcontainer-init command make problem in windows

3- after running this we will have 2 new folders inside local repo (.devcontainer and .vscode)

4- run vscode . command to open the local repo in vscode (it's important because we want to get message -reopen in container- so better to not open directly from vscode and use vscode . from powershell

5- run dev-setup command
6- run git identity 
7- after entering full name and email address we are going for this git accout and repo we go next step
8- run gh auth login command

9- based on our need, we installed development tools from dev-setup app (in my case I added c# development environment into our container) we can see what we installed and activated in our container in .devcontainer.extend/enable-tools.conf (it will be used next time we are rebuiding the project for example in another machine)

10- developing local app and go forward. Act as always! Commit and push and make PR!