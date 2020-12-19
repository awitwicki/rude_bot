import configparser
import os.path

class Config:
    def __init__(self, file_name: str, params: list):
        self.config = configparser.ConfigParser()

        #check if file exists
        if os.path.isfile(file_name):
            self.config.read(file_name)
        else: #create
            print(f'Config file {file_name} not found, creating new one...')

            config = {}

            for param_name in params:
                param_value = input(f'Please type value for {param_name}: ')
                config[param_name] = param_value

            self.config['CONFIG'] = config

            with open(file_name, 'w') as configfile:
                self.config.write(configfile)

            print('Config successfully created')


    @property
    def Data(self):
        return self.config['CONFIG']